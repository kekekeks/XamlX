using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
#if !XAMLIL_INTERNAL
    public
#endif
    class TextNodeEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            if (!(node is XamlXAstTextNode text))
                return null;
            if (!text.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                throw new XamlXLoadException("Text node type wasn't resolved to well-known System.String", node);
            codeGen.Emit(OpCodes.Ldstr, text.Text);
            return XamlXNodeEmitResult.Type(0, text.Type.GetClrType());
        }
    }
}
