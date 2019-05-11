using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXResolvePropertyValueAddersTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstClrProperty prop && prop.Getter != null)
            {
                foreach(var adder in XamlXTransformHelpers.FindPossibleAdders(context, prop.Getter.ReturnType))
                    prop.Setters.Add(new AdderSetter(prop.Getter, adder));
            }

            return node;
        }
        
        class AdderSetter : IXamlXPropertySetter
        {
            private readonly IXamlXMethod _getter;
            private readonly IXamlXMethod _adder;

            public AdderSetter(IXamlXMethod getter, IXamlXMethod adder)
            {
                _getter = getter;
                _adder = adder;
                TargetType = getter.DeclaringType;
                Parameters = adder.ParametersWithThis().Skip(1).ToList();
            }

            public IXamlXType TargetType { get; }

            public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters
            {
                AllowMultiple = true
            };
            
            public IReadOnlyList<IXamlXType> Parameters { get; }
            public void Emit(IXamlXEmitter emitter)
            {
                var locals = new Stack<XamlXLocalsPool.PooledLocal>();
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
