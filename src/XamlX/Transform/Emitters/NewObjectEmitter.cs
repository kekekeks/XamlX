using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
#if !XAMLIL_INTERNAL
    public
#endif
    class NewObjectEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            if (!(node is XamlXAstNewClrObjectNode n))
                return null;

            var type = n.Type.GetClrType();
            var ctor = n.Constructor ?? type.FindConstructor();
            if (ctor == null)
                throw new XamlXLoadException("Unable to find default constructor and no non-default one is specified",
                    n);
            
            for (var c = 0; c < n.Arguments.Count; c++)
            {
                context.Emit(n.Arguments[c], codeGen, ctor.Parameters[c]);
            }

            var gen = codeGen
                .Emit(OpCodes.Newobj, ctor);


            return XamlXNodeEmitResult.Type(0, type);
        }
    }
}
