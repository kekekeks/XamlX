using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class TextNodeEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlAstTextNode text))
                return null;
            if (!text.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                throw new XamlLoadException("Text node type wasn't resolved to well-known System.String", node);
            codeGen.Emit(OpCodes.Ldstr, text.Text);
            return XamlNodeEmitResult.Type(0, text.Type.GetClrType());
        }
    }
}