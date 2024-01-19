using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class XArgumentsTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni)
            {
                var argDirectives = ni.Children.OfType<XamlAstObjectNode>()
                    .Where(d => d.Type is XamlAstXmlTypeReference xref
                                && xref.XmlNamespace == XamlNamespaces.Xaml2006 && xref.Name == "Arguments").ToList();
                if (argDirectives.Count > 1)
                {
                    context.ReportTransformError("x:Arguments directive is specified more than once", argDirectives[1]);
                    return node;
                }
                    
                if (argDirectives.Count == 0)
                    return node;
                ni.Arguments = argDirectives[0].Children.OfType<IXamlAstValueNode>().ToList();
                ni.Children.Remove(argDirectives[0]);

                // This is needed to remove whitespace-only nodes between actual object elements or text nodes
                WhitespaceNormalization.RemoveWhitespaceNodes(ni.Arguments);
            }
            return node;
        }
    }
}
