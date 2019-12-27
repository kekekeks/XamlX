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
                if (argDirectives.Count > 1 && context.StrictMode)
                    throw new XamlParseException("x:Arguments directive is specified more than once",
                        argDirectives[1]);
                if (argDirectives.Count == 0)
                    return node;
                ni.Arguments = argDirectives[0].Children.OfType<IXamlAstValueNode>().ToList();
                ni.Children.Remove(argDirectives[0]);
            }
            return node;
        }
    }
}
