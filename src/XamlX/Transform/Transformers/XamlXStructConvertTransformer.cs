using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXStructConvertTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlAstObjectNode on))
                return node;
            var type = on.Type.GetClrType();
            if (!(type.IsValueType || type.Equals(context.Configuration.WellKnownTypes.String)))
                return node;
            var nonDirectiveChildnren = on.Children.Where(a => !(a is XamlAstXmlDirective)).ToList();
            
            if (on.Arguments.Count != 0
                || nonDirectiveChildnren.Count != 1
                || !(nonDirectiveChildnren[0] is IXamlAstValueNode vn)
                || !vn.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                throw new XamlLoadException(
                    "Value types and System.String can only be loaded via converters. We don't want to mess with ldloca.s, ldflda and other weird stuff",
                    node);

            if (XamlTransformHelpers.TryGetCorrectlyTypedValue(context, vn, on.Type.GetClrType(), out var rv))
            {
                if (nonDirectiveChildnren.Count != on.Children.Count)
                    rv = new XamlValueWithManipulationNode(rv, rv,
                        new XamlManipulationGroupNode(rv, on.Children.OfType<XamlAstXmlDirective>()));
                return rv;
            }
            throw new XamlLoadException(
                $"Unable to convert value {(vn as XamlAstTextNode)?.Text}) to {on.Type.GetClrType()}", vn);
        }
    }
}