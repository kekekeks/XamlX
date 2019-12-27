using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class KnownDirectivesTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni && ni.Type is XamlAstXmlTypeReference type)
            {
                foreach (var d in context.Configuration.KnownDirectives)
                    if (type.XmlNamespace == d.ns && type.Name == d.name)
                    {
                        var vnodes = new List<IXamlAstValueNode>();
                        foreach (var ch in ni.Children)
                        {
                            if(ch is IXamlAstValueNode vn)
                                vnodes.Add(vn);
                            if (context.StrictMode)
                                throw new XamlParseException(
                                    "Only value nodes are allowed as directive children elements", ch);
                            
                        }

                        return new XamlAstXmlDirective(ni, type.XmlNamespace, type.Name, vnodes);
                    }

            }

            return node;
        }
    }
}
