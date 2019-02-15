using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class NewObjectEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlAstNewClrObjectNode n))
                return null;

            var type = n.Type.GetClrType();
            var ctor = n.Constructor ?? type.FindConstructor();
            if (ctor == null)
                throw new XamlLoadException("Unable to find default constructor and no non-default one is specified",
                    n);
            
            for (var c = 0; c < n.Arguments.Count; c++)
            {
                context.Emit(n.Arguments[c], codeGen, ctor.Parameters[c]);
            }

            var gen = codeGen
                .Emit(OpCodes.Newobj, ctor);


            return XamlNodeEmitResult.Type(type);
        }
    }
}