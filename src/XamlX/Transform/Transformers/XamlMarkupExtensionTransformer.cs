using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlMarkupExtensionTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
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
