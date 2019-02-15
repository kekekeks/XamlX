using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXStructConvertTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (!(node is XamlXAstObjectNode on))
                return node;
            var type = on.Type.GetClrType();
            if (!(type.IsValueType || type.Equals(context.Configuration.WellKnownTypes.String)))
                return node;
            if (on.Arguments.Count != 0
                || on.Children.Count != 1
                || !(on.Children[0] is IXamlXAstValueNode vn)
                || !vn.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                throw new XamlXLoadException(
                    "Value types and System.String can only be loaded via converters. We don't want to mess with ldloca.s, ldflda and other weird stuff",
                    node);

            if (XamlXTransformHelpers.TryGetCorrectlyTypedValue(context, vn, on.Type.GetClrType(), out var rv))
                return rv;
            throw new XamlXLoadException(
                $"Unable to convert value {(vn as XamlXAstTextNode)?.Text}) to {on.Type.GetClrType()}", vn);
        }
    }
}