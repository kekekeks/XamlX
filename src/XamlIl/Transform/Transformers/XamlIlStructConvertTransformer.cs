using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlStructConvertTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (!(node is XamlIlAstObjectNode on))
                return node;
            var type = on.Type.GetClrType();
            if (!(type.IsValueType || type.Equals(context.Configuration.WellKnownTypes.String)))
                return node;
            if (on.Arguments.Count != 0
                || on.Children.Count != 1
                || !(on.Children[0] is IXamlIlAstValueNode vn)
                || !vn.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                throw new XamlIlLoadException(
                    "Value types and System.String can only be loaded via converters. We don't want to mess with ldloca.s, ldflda and other weird stuff",
                    node);

            if (context.Configuration.TryGetCorrectlyTypedValue(vn, on.Type.GetClrType(), out var rv))
                return rv;
            throw new XamlIlLoadException(
                $"Unable to convert value {(vn as XamlIlAstTextNode)?.Text}) to {on.Type.GetClrType()}", vn);
        }
    }
}