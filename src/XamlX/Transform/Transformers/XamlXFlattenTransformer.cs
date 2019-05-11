using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXFlattenTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXManipulationGroupNode group && group.Children.Count == 1)
                return group.Children[0];
            return node;
        }
    }
}
