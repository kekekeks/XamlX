using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlFlattenTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlManipulationGroupNode group && group.Children.Count == 1)
                return group.Children[0];
            return node;
        }
    }
}