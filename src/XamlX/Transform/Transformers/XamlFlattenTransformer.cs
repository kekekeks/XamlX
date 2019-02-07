using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlFlattenTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlManipulationGroupNode group && group.Children.Count == 1)
                return group.Children[0];
            return node;
        }
    }
}