using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class TextNodeEmitter : IXamlXAstNodeEmitter
    {
        public bool Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlXAstTextNode text))
                return false;
            if (!text.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                throw new XamlXLoadException("Text node type wasn't resolved to well-known System.String", node);
            codeGen.Generator.Emit(OpCodes.Ldstr, text.Text);
            return true;
        }
    }
}