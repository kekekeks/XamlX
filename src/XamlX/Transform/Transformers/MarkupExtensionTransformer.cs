using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class MarkupExtensionTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is IXamlAstValueNode vn)
            {
                if (context.ParentNodes().FirstOrDefault() is XamlMarkupExtensionNode)
                    return node;
                
                if (XamlTransformHelpers.TryConvertMarkupExtension(context, vn, out var rv))
                    return rv;
            }

            return node;
        }
    }
}
