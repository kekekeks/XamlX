using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXXamlPropertyValueTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstXamlPropertyValueNode pn)
            {
                var assignments = XamlXTransformHelpers.GeneratePropertyAssignments(context,
                    pn.Property.GetClrProperty(),
                    pn.Values);
                return new XamlXManipulationGroupNode(pn)
                {
                    Children = assignments
                };
            }
            else
                return node;
        }
    }
}