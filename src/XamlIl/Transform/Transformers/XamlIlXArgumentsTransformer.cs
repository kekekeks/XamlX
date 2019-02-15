using System.Linq;
using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlXArgumentsTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode ni)
            {
                var argDirectives = ni.Children.OfType<XamlIlAstObjectNode>()
                    .Where(d => d.Type is XamlIlAstXmlTypeReference xref
                                && xref.XmlNamespace == XamlNamespaces.Xaml2006 && xref.Name == "Arguments").ToList();
                if (argDirectives.Count > 1 && context.StrictMode)
                    throw new XamlIlParseException("x:Arguments directive is specified more than once",
                        argDirectives[1]);
                if (argDirectives.Count == 0)
                    return node;
                ni.Arguments = argDirectives[0].Children.OfType<IXamlIlAstValueNode>().ToList();
                ni.Children.Remove(argDirectives[0]);
            }
            return node;
        }
    }
}