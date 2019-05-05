using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXMarkupExtensionTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is IXamlXAstValueNode vn)
            {
                if (context.ParentNodes().FirstOrDefault() is XamlXMarkupExtensionNode)
                    return node;
                
                if (XamlXTransformHelpers.TryConvertMarkupExtension(context, vn, out var rv))
                    return rv;
            }

            return node;
        }
    }
}
