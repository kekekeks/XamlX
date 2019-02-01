using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXXamlPropertyValueTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXamlPropertyValueNode pn)
            {
                var assignments = XamlTransformHelpers.GeneratePropertyAssignments(context,
                    pn.Property.GetClrProperty(),
                    pn.Values);
                return new XamlManipulationGroupNode(pn)
                {
                    Children = assignments
                };
            }
            else
                return node;
        }
    }
}