using System.Linq;
using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlContentConvertTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (!(node is XamlIlAstObjectNode on))
                return node;
            var nonDirectiveChildren = on.Children.Where(a => !(a is XamlIlAstXmlDirective)).ToList();

            if (on.Arguments.Count != 0
                || nonDirectiveChildren.Count != 1
                || !(nonDirectiveChildren[0] is IXamlIlAstValueNode vn)
                || !vn.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                return node;
            
            if (XamlIlTransformHelpers.TryGetCorrectlyTypedValue(context, vn, on.Type.GetClrType(), out var rv))
            {
                if (nonDirectiveChildren.Count != on.Children.Count)
                    rv = new XamlIlValueWithManipulationNode(rv, rv,
                        new XamlIlManipulationGroupNode(rv, on.Children.OfType<XamlIlAstXmlDirective>()));
                return rv;
            }

            if (on.Type.GetClrType().IsValueType)
                throw new XamlIlLoadException(
                    $"Unable to convert value {(vn as XamlIlAstTextNode)?.Text}) to {on.Type.GetClrType()}", vn);
            
            // Parser not found, isn't a value type, probably a regular object creation node with text content
            return node;
        }
    }
}