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
            if (on.Arguments.Count != 0
                || on.Children.Count != 1
                || !(on.Children[0] is IXamlAstValueNode vn)
                || !vn.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                throw new XamlLoadException(
                    "Value types and System.String can only be loaded via converters. We don't want to mess with ldloca.s, ldflda and other weird stuff",
                    node);

            if (XamlTransformHelpers.TryGetCorrectlyTypedValue(context, vn, on.Type.GetClrType(), out var rv))
                return rv;
            throw new XamlLoadException(
                $"Unable to convert value {(vn as XamlAstTextNode)?.Text}) to {on.Type.GetClrType()}", vn);
        }
    }
}