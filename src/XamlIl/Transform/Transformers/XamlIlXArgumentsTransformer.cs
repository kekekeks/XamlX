using System.Linq;
using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlXArgumentsTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstNewInstanceNode ni)
            {
                var argDirectives = ni.Children.OfType<XamlIlAstXmlDirective>()
                    .Where(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Arguments").ToList();
                if (argDirectives.Count > 1 && context.StrictMode)
                    throw new XamlIlParseException("x:Arguments directive is specified more than once",
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