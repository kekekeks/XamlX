using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlKnownDirectivesTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode ni && ni.Type is XamlIlAstXmlTypeReference type)
            {
                foreach (var d in context.Configuration.KnownDirectives)
                    if (type.XmlNamespace == d.ns && type.Name == d.name)
                    {
                        var vnodes = new List<IXamlIlAstValueNode>();
                        foreach (var ch in ni.Children)
                        {
                            if(ch is IXamlIlAstValueNode vn)
                                vnodes.Add(vn);
                            if (context.StrictMode)
                                throw new XamlIlParseException(
                                    "Only value nodes are allowed as directive children elements", ch);
                            
                        }

                        return new XamlIlAstXmlDirective(ni, type.XmlNamespace, type.Name, vnodes);
                    }

            }

            return node;
        }
    }
}