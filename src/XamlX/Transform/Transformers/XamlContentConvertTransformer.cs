using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlContentConvertTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlAstObjectNode on))
                return node;
            var nonDirectiveChildnren = on.Children.Where(a => !(a is XamlAstXmlDirective)).ToList();

            if (on.Arguments.Count != 0
                || nonDirectiveChildnren.Count != 1
                || !(nonDirectiveChildnren[0] is IXamlAstValueNode vn)
                || !vn.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                return node;
            
            if (XamlTransformHelpers.TryGetCorrectlyTypedValue(context, vn, on.Type.GetClrType(), out var rv))
            {
                if (nonDirectiveChildnren.Count != on.Children.Count)
                    rv = new XamlValueWithManipulationNode(rv, rv,
                        new XamlManipulationGroupNode(rv, on.Children.OfType<XamlAstXmlDirective>()));
                return rv;
            }

            if (on.Type.GetClrType().IsValueType)
                throw new XamlLoadException(
                    $"Unable to convert value {(vn as XamlAstTextNode)?.Text}) to {on.Type.GetClrType()}", vn);
            
            // Parser not found, isn't a value type, probably a regular object creation node with text content
            return node;
        }
    }
}