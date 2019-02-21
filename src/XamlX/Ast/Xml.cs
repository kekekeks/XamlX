using System.Collections.Generic;
using System.Linq;
using Visitor = XamlX.Ast.IXamlXAstVisitor;
namespace XamlX.Ast
{
    public class XamlXAstXmlTypeReference : XamlXAstNode, IXamlXAstTypeReference
    {
        public string XmlNamespace { get; set; }
        public string Name { get; set; }
        public List<XamlXAstXmlTypeReference> GenericArguments { get; set; } = new List<XamlXAstXmlTypeReference>();

        public XamlXAstXmlTypeReference(IXamlXLineInfo lineInfo, string xmlNamespace, string name,
            IEnumerable<XamlXAstXmlTypeReference> genericArguments = null) : base(lineInfo)
        {
            XmlNamespace = xmlNamespace;
            Name = name;
            GenericArguments = genericArguments?.ToList() ?? GenericArguments;
        }

        public override void VisitChildren(Visitor visitor) => VisitList(GenericArguments, visitor);
        public override string ToString() => "xml!!" + XmlNamespace + ":" + Name;
    }
}
