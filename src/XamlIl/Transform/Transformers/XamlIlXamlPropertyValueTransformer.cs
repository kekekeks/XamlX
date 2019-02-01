using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlXamlPropertyValueTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstXamlPropertyValueNode pn)
            {
                var assignments = XamlIlTransformHelpers.GeneratePropertyAssignments(context,
                    pn.Property.GetClrProperty(),
                    pn.Values);
                return new XamlIlManipulationGroupNode(pn)
                {
                    Children = assignments
                };
            }
            else
                return node;
        }
    }
}