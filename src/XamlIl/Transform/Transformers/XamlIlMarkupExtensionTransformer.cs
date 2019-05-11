using System.Linq;
using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlMarkupExtensionTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is IXamlIlAstValueNode vn)
            {
                if (context.ParentNodes().FirstOrDefault() is XamlIlMarkupExtensionNode)
                    return node;
                
                if (XamlIlTransformHelpers.TryConvertMarkupExtension(context, vn, out var rv))
                    return rv;
            }

            return node;
        }
    }
}
