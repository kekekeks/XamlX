using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXContentConvertTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (!(node is XamlXAstObjectNode on))
                return node;
            var nonDirectiveChildnren = on.Children.Where(a => !(a is XamlXAstXmlDirective)).ToList();

            if (on.Arguments.Count != 0
                || nonDirectiveChildnren.Count != 1
                || !(nonDirectiveChildnren[0] is IXamlXAstValueNode vn)
                || !vn.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                return node;
            
            if (XamlXTransformHelpers.TryGetCorrectlyTypedValue(context, vn, on.Type.GetClrType(), out var rv))
            {
                if (nonDirectiveChildnren.Count != on.Children.Count)
                    rv = new XamlXValueWithManipulationNode(rv, rv,
                        new XamlXManipulationGroupNode(rv, on.Children.OfType<XamlXAstXmlDirective>()));
                return rv;
            }

            if (on.Type.GetClrType().IsValueType)
                throw new XamlXLoadException(
                    $"Unable to convert value {(vn as XamlXAstTextNode)?.Text}) to {on.Type.GetClrType()}", vn);
            
            // Parser not found, isn't a value type, probably a regular object creation node with text content
            return node;
        }
    }
}