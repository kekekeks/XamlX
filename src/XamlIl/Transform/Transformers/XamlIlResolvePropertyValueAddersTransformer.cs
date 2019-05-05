using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlResolvePropertyValueAddersTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstClrProperty prop && prop.Getter != null)
            {
                foreach(var adder in XamlIlTransformHelpers.FindPossibleAdders(context, prop.Getter.ReturnType))
                    prop.Setters.Add(new AdderSetter(prop.Getter, adder));
            }

            return node;
        }
        
        class AdderSetter : IXamlIlPropertySetter
        {
            private readonly IXamlIlMethod _getter;
            private readonly IXamlIlMethod _adder;

            public AdderSetter(IXamlIlMethod getter, IXamlIlMethod adder)
            {
                _getter = getter;
                _adder = adder;
                TargetType = getter.DeclaringType;
                Parameters = adder.ParametersWithThis().Skip(1).ToList();
            }

            public IXamlIlType TargetType { get; }

            public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters
            {
                AllowMultiple = true
            };
            
            public IReadOnlyList<IXamlIlType> Parameters { get; }
            public void Emit(IXamlIlEmitter emitter)
            {
                var locals = new Stack<XamlIlLocalsPool.PooledLocal>();
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
