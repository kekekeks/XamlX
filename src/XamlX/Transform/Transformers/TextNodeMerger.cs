using System.Text;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    // Merges adjacent text nodes
#if !XAMLX_INTERNAL
    public
#endif
    class TextNodeMerger : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode objectNode)
            {
                var nextNodeIsTextNode = false;
                for (var i = objectNode.Children.Count - 1; i >= 0; i--)
                {
                    var childNode = objectNode.Children[i];
                    if (childNode is XamlAstTextNode textNode)
                    {
                        // If childNode is the first node in a chain of text nodes, merge it with all subsequent
                        // text nodes, and remove them.
                        if (nextNodeIsTextNode && (i == 0 || !(objectNode.Children[i - 1] is XamlAstTextNode)))
                        {
                            var newText = new StringBuilder(textNode.Text);
                            while (i + 1 < objectNode.Children.Count && objectNode.Children[i + 1] is XamlAstTextNode nextTextNode)
                            {
                                newText.Append(nextTextNode.Text);
                                objectNode.Children.RemoveAt(i + 1);
                            }

                            textNode.Text = newText.ToString();
                        }
                        nextNodeIsTextNode = true;
                    }
                    else
                    {
                        nextNodeIsTextNode = false;
                    }
                }
            }

            return node;
        }
    }
}