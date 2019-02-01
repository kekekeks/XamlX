using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXKnownContentDirectivesTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlXAstNewInstanceNode ni && ni.Type is XamlAstXmlTypeReference type)
            {
                foreach (var d in context.Configuration.KnownContentDirectives)
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