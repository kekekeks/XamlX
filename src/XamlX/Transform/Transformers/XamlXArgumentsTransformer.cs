using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXArgumentsTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni)
            {
                var argDirectives = ni.Children.OfType<XamlAstXmlDirective>()
                    .Where(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Arguments").ToList();
                if (argDirectives.Count > 1 && context.StrictMode)
                    throw new XamlParseException("x:Arguments directive is specified more than once",
                        argDirectives[1]);
                if (argDirectives.Count == 0)
                    return node;
                ni.Arguments = argDirectives[0].Values;
                ni.Children.Remove(argDirectives[0]);
            }
            return node;
        }
    }
}