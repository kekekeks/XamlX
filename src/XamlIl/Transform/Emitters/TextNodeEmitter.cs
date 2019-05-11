using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
#if !XAMLIL_INTERNAL
    public
#endif
    class TextNodeEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!(node is XamlIlAstTextNode text))
                return null;
            if (!text.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                throw new XamlIlLoadException("Text node type wasn't resolved to well-known System.String", node);
            codeGen.Emit(OpCodes.Ldstr, text.Text);
            return XamlIlNodeEmitResult.Type(0, text.Type.GetClrType());
        }
    }
}
