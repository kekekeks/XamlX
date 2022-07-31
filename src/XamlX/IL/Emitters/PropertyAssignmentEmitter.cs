using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.TypeSystem;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    class PropertyAssignmentEmitter : IXamlAstLocalsNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        List<IXamlPropertySetter> ValidateAndGetSetters(XamlPropertyAssignmentNode an)
        {
            var lst = an.PossibleSetters.Where(x => x.Parameters.Count == an.Values.Count).ToList();
            if(an.Values.Count>1 && lst.Count>1)
                for (var c = 0; c < an.Values.Count - 2; c++)
                {
                    var failed = an.PossibleSetters.FirstOrDefault(x =>
                        !x.Parameters[c].IsDirectlyAssignableFrom(an.Values[c].Type.GetClrType()));
                    if (failed != null)
                    {
                        throw new XamlLoadException(
                            $"Can not statically cast {an.Values[c].Type.GetClrType().GetFqn()} to {failed.Parameters[c].GetFqn()} and runtime type checking is only supported for the last setter argument",
                            an);
                    }
                }

            if (lst.Count == 0)
                throw new XamlLoadException("No setters found for property assignment", an);
            return lst;
        }

        public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (node is not XamlPropertyAssignmentNode an)
                return null;

            var setters = ValidateAndGetSetters(an);
            for (var c = 0; c < an.Values.Count - 1; c++)
                context.Emit(an.Values[c], codeGen, an.Values[c].Type.GetClrType());

            var dynamicValue = an.Values.Last();
            var dynamicValueType = dynamicValue.Type.GetClrType();

            RemoveRedundantSetters(dynamicValueType, setters);

            if (setters.Count == 1)
            {
                var setter = setters[0];
                context.Emit(dynamicValue, codeGen, setter.Parameters.Last());
                context.Emit(setter, codeGen);
            }
            else
            {
                var valueTypes = an.Values.Select(x => x.Type.GetClrType()).ToArray();
                var method = GetOrCreateDynamicSetterMethod(an.Property.DeclaringType, valueTypes, setters, context);
                context.Emit(dynamicValue, codeGen, dynamicValueType);
                codeGen.EmitCall(method);
            }

            return XamlILNodeEmitResult.Void(1);
        }

        private static void RemoveRedundantSetters(IXamlType valueType, List<IXamlPropertySetter> setters)
        {
            if (setters.Count == 1)
                return;

            // If the value is a value type, always use the first one
            if (valueType.IsValueType)
            {
                setters.RemoveRange(1, setters.Count - 1);
                return;
            }

            for (int index = 0; index < setters.Count;)
            {
                var setter = setters[index];
                var type = setter.Parameters.Last();

                // the value is directly assignable by downcast and the setter allows null: it will always match
                if (type.IsAssignableFrom(valueType) && setter.BinderParameters.AllowRuntimeNull)
                {
                    setters.RemoveRange(index + 1, setters.Count - index - 1);
                    return;
                }

                // the value type has already been handled by a previous setter
                if (setters.Take(index).Any(previous => IsAssignableToWithNullability(setter, previous)))
                {
                    setters.RemoveAt(index);
                    continue;
                }

                ++index;
            }
        }

        private static bool IsAssignableToWithNullability(IXamlPropertySetter from, IXamlPropertySetter to)
            => to.Parameters.Last().IsAssignableFrom(from.Parameters.Last())
               && (to.BinderParameters.AllowRuntimeNull || !from.BinderParameters.AllowRuntimeNull);

        private static IXamlMethod GetOrCreateDynamicSetterMethod(
            IXamlType parentType,
            IReadOnlyList<IXamlType> valueTypes,
            IReadOnlyList<IXamlPropertySetter> setters,
            XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context)
        {
            if (!context.TryGetItem(out DynamicSettersCache cache))
            {
                var settersType = context.CreateSubType(
                    "DynamicSetters_" + context.Configuration.IdentifierGenerator.GenerateIdentifierPart(),
                    context.Configuration.WellKnownTypes.Object);
                cache = new DynamicSettersCache(settersType);
                context.SetItem(cache);
                context.AddAfterEmitCallbacks(() => settersType.CreateType());
            }

            var cacheKey = new SettersCacheKey(parentType, valueTypes, setters);

            if (!cache.MethodByCacheKey.TryGetValue(cacheKey, out var method))
            {
                method = cache.SettersType.DefineMethod(
                    context.Configuration.WellKnownTypes.Void,
                    new[] { parentType }.Concat(valueTypes),
                    "DynamicSetter_" + (cache.MethodByCacheKey.Count + 1),
                    true, true, false);

                var newContext = new ILEmitContext(
                    method.Generator, context.Configuration, context.EmitMappings, context.RuntimeContext,
                    null,
                    (s, type) => cache.SettersType.DefineSubType(type, s, false),
                    (s, returnType, parameters) => cache.SettersType.DefineDelegateSubType(s, false, returnType, parameters),
                    context.File,
                    context.Emitters);

                EmitDynamicSetterMethod(valueTypes, setters, newContext);

                cache.MethodByCacheKey[cacheKey] = method;
            }

            return method;
        }

        private static void EmitDynamicSetterMethod(
            IReadOnlyList<IXamlType> valueTypes,
            IReadOnlyList<IXamlPropertySetter> setters,
            XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context)
        {
            var codeGen = context.Emitter;

            codeGen.Ldarg_0();
            for (int i = 0; i < valueTypes.Count; ++i)
                codeGen.Ldarg(i + 1);

            var dynamicValueType = valueTypes.Last();
            IXamlLabel firstAllowingNull = null;
            IXamlLabel next = null;

            foreach (var setter in setters)
            {
                if (next != null)
                {
                    codeGen.MarkLabel(next);
                    next = null;
                }

                // Only do dynamic checks if we know that type is not assignable by downcast
                var type = setter.Parameters.Last();
                if (!type.IsAssignableFrom(dynamicValueType))
                {
                    next = codeGen.DefineLabel();

                    codeGen
                        .Dup()
                        .Isinst(type)
                        .Brfalse(next);

                    if (type.IsValueType)
                        codeGen.Unbox_Any(type);
                }
                else if (!setter.BinderParameters.AllowRuntimeNull)
                {
                    next = codeGen.DefineLabel();

                    codeGen
                        .Dup()
                        .Brfalse(next);
                }

                if (next != null)
                {
                    if (setter.BinderParameters.AllowRuntimeNull && firstAllowingNull == null)
                    {
                        firstAllowingNull = codeGen.DefineLabel();
                        codeGen.MarkLabel(firstAllowingNull);
                    }
                }

                context.Emit(setter, codeGen);
                codeGen.Ret();

                if (next == null)
                    break;
            }

            if (next != null)
            {
                codeGen.MarkLabel(next);

                // the value didn't match any type, but it may be null, if so jump to the first setter allowing null
                if (firstAllowingNull != null)
                {
                    codeGen
                        .Dup()
                        .Brfalse(firstAllowingNull);
                }
                else
                {
                    next = codeGen.DefineLabel();
                    codeGen
                        .Dup()
                        .Brtrue(next)
                        .Newobj(context.Configuration.TypeSystem.GetType("System.NullReferenceException")
                            .FindConstructor())
                        .Throw();
                    codeGen.MarkLabel(next);
                }

                codeGen
                    .Newobj(context.Configuration.TypeSystem.GetType("System.InvalidCastException")
                        .FindConstructor())
                    .Throw();
            }
        }

        private readonly struct SettersCacheKey : IEquatable<SettersCacheKey>
        {
            public IXamlType ParentType { get; }
            public IReadOnlyList<IXamlType> ValueTypes { get; }
            public IReadOnlyList<IXamlPropertySetter> Setters { get; }

            private static int GetListHashCode<T>(IReadOnlyList<T> list)
            {
                int hashCode = list.Count;
                for (var i = 0; i < list.Count; ++i)
                    hashCode = (hashCode * 397) ^ list[i].GetHashCode();
                return hashCode;
            }

            private static bool AreListEqual<T>(IReadOnlyList<T> x, IReadOnlyList<T> y)
            {
                if (x.Count != y.Count)
                    return false;

                for (var i = 0; i < x.Count; ++i)
                {
                    if (!EqualityComparer<T>.Default.Equals(x[i], y[i]))
                        return false;
                }

                return true;
            }

            public bool Equals(SettersCacheKey other)
                => ParentType == other.ParentType
                   && AreListEqual(ValueTypes, other.ValueTypes)
                   && AreListEqual(Setters, other.Setters);

            public override bool Equals(object obj)
                => obj is SettersCacheKey other && Equals(other);

            public override int GetHashCode()
            {
                var hashCode = ParentType.GetHashCode();
                hashCode = (hashCode * 397) ^ GetListHashCode(ValueTypes);
                hashCode = (hashCode * 397) ^ GetListHashCode(Setters);
                return hashCode;
            }

            public SettersCacheKey(IXamlType parentType, IReadOnlyList<IXamlType> valueTypes, IReadOnlyList<IXamlPropertySetter> setters)
            {
                ParentType = parentType;
                ValueTypes = valueTypes;
                Setters = setters;
            }
        }

        private sealed class DynamicSettersCache
        {
            public IXamlTypeBuilder<IXamlILEmitter> SettersType { get; }

            public Dictionary<SettersCacheKey, IXamlMethodBuilder<IXamlILEmitter>> MethodByCacheKey { get; } = new();

            public DynamicSettersCache(IXamlTypeBuilder<IXamlILEmitter> settersType)
                => SettersType = settersType;
        }
    }
}
