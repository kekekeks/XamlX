using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    /// <summary>
    /// This transformer drops insignificant whitespace before and between <see cref="XamlAstXamlPropertyValueNode">
    /// AST property value nodes</see> within <see cref="XamlAstObjectNode">AST object nodes</see>.
    /// </summary>
#if !XAMLX_INTERNAL
    public
#endif
    class RemoveWhitespaceBetweenPropertyValuesTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            var propertyEncountered = false;
            if (node is XamlAstObjectNode ni)
            {
                for (var c = ni.Children.Count - 1; c >= 0; c--)
                {
                    var child = ni.Children[c];
                    if (child is XamlAstXamlPropertyValueNode)
                    {
                        propertyEncountered = true;
                    }
                    else if (propertyEncountered && child is XamlAstTextNode textNode)
                    {
                        if (WhitespaceNormalization.IsWhitespace(textNode.Text))
                        {
                            ni.Children.RemoveAt(c);
                        }
                    }
                }
            }

            return node;
        }
    }
}