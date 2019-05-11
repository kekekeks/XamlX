using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXContentConvertTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (!(node is XamlXAstObjectNode on))
                return node;
            var nonDirectiveChildren = on.Children.Where(a => !(a is XamlXAstXmlDirective)).ToList();

            if (on.Arguments.Count != 0
                || nonDirectiveChildren.Count != 1
                || !(nonDirectiveChildren[0] is IXamlXAstValueNode vn)
                || !vn.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                return node;
            
            if (XamlXTransformHelpers.TryGetCorrectlyTypedValue(context, vn, on.Type.GetClrType(), out var rv))
            {
                if (nonDirectiveChildren.Count != on.Children.Count)
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
