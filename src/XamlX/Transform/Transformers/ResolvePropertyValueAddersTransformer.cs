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
        
        class AdderSetter : IXamlPropertySetter, IXamlEmitablePropertySetter<IXamlILEmitter>
        {
            private readonly IXamlMethod _getter;
            private readonly IXamlMethod _adder;

            public AdderSetter(IXamlMethod getter, IXamlMethod adder)
            {
                _getter = getter;
                _adder = adder;
                TargetType = getter.DeclaringType;
                Parameters = adder.ParametersWithThis().Skip(1).ToList();
            }

            public IXamlType TargetType { get; }
            public IXamlType ParameterType => Parameters.Last();

            public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters
            {
                AllowMultiple = true
            };
            
            public IReadOnlyList<IXamlType> Parameters { get; }

            public bool Matches(IReadOnlyList<IXamlAstValueNode> arguments)
            {
                var parameters = _adder.ParametersWithThis().Skip(1).ToList();

                if (arguments.Count == parameters.Count)
                {
                    for (var i = 0; i < parameters.Count; ++i)
                    {
                        var argument = arguments[i];
                        var parameter = parameters[i];

                        // Don't allow x:Null
                        if (!BinderParameters.AllowXNull && XamlPseudoType.Null.Equals(argument.Type))
                            return false;

                        // Direct cast
                        if (parameter.IsAssignableFrom(argument.Type.GetClrType()))
                            return true;

                        // Upcast from System.Object
                        //if (argument.Equals(_config.WellKnownTypes.Object))
                        //    return true;
                    }
                }

                return false;
            }

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
        }
    }
}
