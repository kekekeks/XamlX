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

        public XamlILNodeEmitResult? Emit(IXamlAstNode node, XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (node is not XamlPropertyAssignmentNode an)
                return null;

            var dynamicValue = an.Values.Last();
            var dynamicValueType = dynamicValue.Type.GetClrType();

            var setters = ValidateAndGetSetters(an);
            RemoveRedundantSetters(dynamicValueType, setters);

            if (setters.Count == 1)
            {
                var setter = setters[0];

                if (setter is IXamlILOptimizedEmitablePropertySetter optimizedSetter)
                    optimizedSetter.EmitWithArguments(context, codeGen, an.Values);
                else
                {
                    for (var i = 0; i < an.Values.Count - 1; ++i)
                        context.Emit(an.Values[i], codeGen, an.Values[i].Type.GetClrType());
                    context.Emit(dynamicValue, codeGen, setter.Parameters[setter.Parameters.Count - 1]);
                    context.Emit(setter, codeGen);
                }
            }
            else
            {
                var valueTypes = an.Values.Select(x => x.Type.GetClrType()).ToArray();
                var method = GetOrCreateDynamicSetterMethod(an.Property, valueTypes, setters, dynamicValue, context);

                for (var i = 0; i < an.Values.Count - 1; ++i)
                    context.Emit(an.Values[i], codeGen, an.Values[i].Type.GetClrType());
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
                var type = setter.Parameters[setter.Parameters.Count - 1];

                // the value is directly assignable by downcast and the setter allows null: it will always match
                if (type.IsAssignableFrom(valueType) && setter.BinderParameters.AllowRuntimeNull)
                {
                    setters.RemoveRange(index + 1, setters.Count - index - 1);
                    return;
                }

                // we have already found a previous setter that matches the value's type or its base type
                if (setters.Take(index).Any(previous => IsAssignableToWithNullability(setter, previous)))
                {
                    setters.RemoveAt(index);
                    continue;
                }

                ++index;
            }
        }

        private static bool IsAssignableToWithNullability(IXamlPropertySetter from, IXamlPropertySetter to)
            => to.Parameters[to.Parameters.Count - 1].IsAssignableFrom(from.Parameters[from.Parameters.Count - 1])
               && (to.BinderParameters.AllowRuntimeNull || !from.BinderParameters.AllowRuntimeNull);

        private static IXamlMethod GetOrCreateDynamicSetterMethod(
            XamlAstClrProperty property,
            IReadOnlyList<IXamlType> valueTypes,
            IReadOnlyList<IXamlPropertySetter> setters,
            IXamlLineInfo lineInfo,
            XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context)
        {
            var containerProvider = context.Configuration.GetOrCreateExtra<ILEmitContextSettings>().DynamicSetterContainerProvider;
            var container = containerProvider.ProvideDynamicSetterContainer(property, context);
            var typeBuilder = container.TypeBuilder;

            var typeCache = context.Configuration.GetOrCreateExtra<DynamicSettersTypeCache>();
            if (!typeCache.MethodCacheByType.TryGetValue(typeBuilder, out var methodCache))
            {
                methodCache = new DynamicSettersMethodCache();
                typeCache.MethodCacheByType[typeBuilder] = methodCache;
            }

            var declaringType = property.DeclaringType;
            var cacheKey = new SettersCacheKey(declaringType, valueTypes, setters);

            if (!methodCache.MethodByCacheKey.TryGetValue(cacheKey, out var method))
            {
                method = typeBuilder.DefineMethod(
                    context.Configuration.WellKnownTypes.Void,
                    new[] { property.DeclaringType }.Concat(valueTypes),
                    container.GetDynamicSetterMethodName(methodCache.MethodByCacheKey.Count),
                    container.GeneratedMethodsVisibility,
                    true,
                    false);

                var newContext = new ILEmitContext(
                    method.Generator,
                    context.Configuration,
                    context.EmitMappings,
                    context.RuntimeContext,
                    null,
                    typeBuilder,
                    context.File,
                    context.Emitters);

                EmitDynamicSetterMethod(valueTypes, setters, lineInfo, newContext);

                methodCache.MethodByCacheKey[cacheKey] = method;
            }

            return method;
        }

        private static void EmitDynamicSetterMethod(
            IReadOnlyList<IXamlType> valueTypes,
            IReadOnlyList<IXamlPropertySetter> setters,
            IXamlLineInfo lineInfo,
            XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context)
        {
            var codeGen = context.Emitter;

            codeGen.Ldarg_0();
            for (int i = 0; i < valueTypes.Count; ++i)
                codeGen.Ldarg(i + 1);

            var dynamicValueType = valueTypes[valueTypes.Count - 1];
            IXamlPropertySetter? firstSetterAllowingNull = null;
            IXamlLabel? next = null;

            void EmitSetterAfterChecks(IXamlPropertySetter setter, IXamlType typeOnStack)
            {
                // Convert is needed for T to T? and null to T?, will be a no-op in other cases
                ILEmitHelpers.EmitConvert(context, codeGen, lineInfo, typeOnStack, setter.Parameters[setter.Parameters.Count - 1]);
                context.Emit(setter, codeGen);
                codeGen.Ret();
            }

            foreach (var setter in setters)
            {
                if (setter.BinderParameters.AllowRuntimeNull)
                    firstSetterAllowingNull ??= setter;

                if (next != null)
                {
                    codeGen.MarkLabel(next);
                    next = null;
                }

                var parameterType = setter.Parameters[setter.Parameters.Count - 1];
                IXamlType typeOnStack = dynamicValueType;

                // Only do dynamic checks if we know that the value is not assignable by downcast
                if (!parameterType.IsAssignableFrom(dynamicValueType))
                {
                    // for Nullable<T>, check if the value is a T, null is handled later
                    var checkedType = parameterType.IsNullable() ? parameterType.GenericArguments[0] : parameterType;

                    next = codeGen.DefineLabel();

                    codeGen
                        .Dup()
                        .Isinst(checkedType)
                        .Brfalse(next);

                    if (checkedType.IsValueType)
                        codeGen.Unbox_Any(checkedType);

                    typeOnStack = checkedType;
                }
                else if (!setter.BinderParameters.AllowRuntimeNull)
                {
                    next = codeGen.DefineLabel();

                    codeGen
                        .Dup()
                        .Brfalse(next);
                }

                EmitSetterAfterChecks(setter, typeOnStack);

                if (next == null)
                    break;
            }

            if (next != null)
            {
                codeGen.MarkLabel(next);

                // the value didn't match any type, but it may be null: either emit a setter allowing null, or throw
                next = codeGen.DefineLabel();
                codeGen
                    .Dup()
                    .Brtrue(next);

                if (firstSetterAllowingNull != null)
                    EmitSetterAfterChecks(firstSetterAllowingNull, XamlPseudoType.Null);
                else
                {
                    codeGen
                        .Newobj(context.Configuration.TypeSystem.GetType("System.NullReferenceException")
                            .GetConstructor())
                        .Throw();
                }

                codeGen.MarkLabel(next);

                codeGen
                    .Newobj(context.Configuration.TypeSystem.GetType("System.InvalidCastException")
                        .GetConstructor())
                    .Throw();
            }
        }

        private readonly struct SettersCacheKey : IEquatable<SettersCacheKey>
        {
            public IXamlType ParentType { get; }
            public IReadOnlyList<IXamlType> ValueTypes { get; }
            public IReadOnlyList<IXamlPropertySetter> Setters { get; }

            private static int GetListHashCode<T>(IReadOnlyList<T> list)
                where T : notnull
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
                => ParentType.Equals(other.ParentType)
                   && AreListEqual(ValueTypes, other.ValueTypes)
                   && AreListEqual(Setters, other.Setters);

            public override bool Equals(object? obj)
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

        private sealed class DynamicSettersTypeCache
        {
            public Dictionary<IXamlTypeBuilder<IXamlILEmitter>, DynamicSettersMethodCache> MethodCacheByType { get; } = new();
        }

        private sealed class DynamicSettersMethodCache
        {
            public Dictionary<SettersCacheKey, IXamlMethodBuilder<IXamlILEmitter>> MethodByCacheKey { get; } = new();
        }
    }
}
