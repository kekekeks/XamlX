using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class TextNodeEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (!(node is XamlIlAstTextNode text))
                return null;
            if (!text.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                throw new XamlIlLoadException("Text node type wasn't resolved to well-known System.String", node);
            codeGen.Generator.Emit(OpCodes.Ldstr, text.Text);
            return XamlIlNodeEmitResult.Type(text.Type.GetClrType());
        }
    }
}