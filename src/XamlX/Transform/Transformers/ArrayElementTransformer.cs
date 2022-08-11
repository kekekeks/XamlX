using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class ArrayElementTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlArrayExtensionNode array)
            {
                var elementType = array.Type.GetClrType().ArrayElementType;

                for (int i = 0; i < array.Elements.Count; i++)
                {
                    var element = array.Elements[i];

                    if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, element, elementType, out var converted))
                    {
                        throw new XamlLoadException(
                            $"Unable to convert value {element} to x:Array element type {elementType.GetFqn()}", element);
                    }

                    array.Elements[i] = converted;
                }
            }

            return node;
        }
    }
}
