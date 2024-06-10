using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class ResolvePropertyValueAddersTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstClrProperty prop && prop.Getter != null)
            {
                foreach(var adder in XamlTransformHelpers.FindPossibleAdders(context, prop.Getter.ReturnType))
                    prop.Setters.Add(new AdderSetter(prop.Getter, adder));
            }

            return node;
        }
        
        class AdderSetter : IXamlILOptimizedEmitablePropertySetter, IEquatable<AdderSetter>
        {
            private readonly IXamlMethod _getter;
            private readonly IXamlMethod _adder;

            public AdderSetter(IXamlMethod getter, IXamlMethod adder)
            {
                _getter = getter;
                _adder = adder;
                TargetType = getter.DeclaringType;
                Parameters = adder.ParametersWithThis().Skip(1).ToList();

                bool allowNull = Parameters[Parameters.Count - 1].AcceptsNull();
                BinderParameters = new PropertySetterBinderParameters
                {
                    AllowMultiple = true,
                    AllowXNull = allowNull,
                    AllowRuntimeNull = allowNull,
                    AllowAttributeSyntax = false,
                };
            }

            public IXamlType TargetType { get; }

            public PropertySetterBinderParameters BinderParameters { get; }
            
            public IReadOnlyList<IXamlType> Parameters { get; }
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _adder.CustomAttributes;

            public void Emit(IXamlILEmitter emitter)
            {
                var locals = new Stack<XamlLocalsPool.PooledLocal>();
                // Save all "setter" parameters
                for (var c = Parameters.Count - 1; c >= 0; c--)
                {
                    var loc = emitter.LocalsPool.GetLocal(Parameters[c]);
                    locals.Push(loc);
                    emitter.Stloc(loc.Local);
                }

                emitter.EmitCall(_getter);
                while (locals.Count>0)
                    using (var loc = locals.Pop())
                        emitter.Ldloc(loc.Local);
                emitter.EmitCall(_adder, true);
            }

            public void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                emitter.EmitCall(_getter);

                for (var i = 0; i < arguments.Count; ++i)
                    context.Emit(arguments[i], emitter, Parameters[i]);

                emitter.EmitCall(_adder, true);
            }

            public bool Equals(AdderSetter? other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;

                return _getter.Equals(other._getter) && _adder.Equals(other._adder);
            }

            public override bool Equals(object? obj)
                => Equals(obj as AdderSetter);

            public override int GetHashCode() 
                => (_getter.GetHashCode() * 397) ^ _adder.GetHashCode();
        }
    }
}
