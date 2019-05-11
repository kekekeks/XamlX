using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXXArgumentsTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstObjectNode ni)
            {
                var argDirectives = ni.Children.OfType<XamlXAstObjectNode>()
                    .Where(d => d.Type is XamlXAstXmlTypeReference xref
                                && xref.XmlNamespace == XamlNamespaces.Xaml2006 && xref.Name == "Arguments").ToList();
                if (argDirectives.Count > 1 && context.StrictMode)
                    throw new XamlXParseException("x:Arguments directive is specified more than once",
                        argDirectives[1]);
                if (argDirectives.Count == 0)
                    return node;
                ni.Arguments = argDirectives[0].Children.OfType<IXamlXAstValueNode>().ToList();
                ni.Children.Remove(argDirectives[0]);
            }
            return node;
        }
    }
}
