using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class FlattenAstTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlManipulationGroupNode group && group.Children.Count == 1)
                return group.Children[0];
            return node;
        }
    }
}
