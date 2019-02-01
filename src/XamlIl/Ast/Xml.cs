using System.Collections.Generic;
using System.Linq;
using Visitor = XamlIl.Ast.XamlIlAstVisitorDelegate;
namespace XamlIl.Ast
{
    public class XamlIlAstXmlTypeReference : XamlIlAstNode, IXamlIlAstTypeReference
    {
        public string XmlNamespace { get; set; }
        public string Name { get; set; }
        public List<XamlIlAstXmlTypeReference> GenericArguments { get; set; } = new List<XamlIlAstXmlTypeReference>();

        public XamlIlAstXmlTypeReference(IXamlIlLineInfo lineInfo, string xmlNamespace, string name,
            IEnumerable<XamlIlAstXmlTypeReference> genericArguments = null) : base(lineInfo)
        {
            XmlNamespace = xmlNamespace;
            Name = name;
            GenericArguments = genericArguments?.ToList() ?? GenericArguments;
        }

        public override void VisitChildren(Visitor visitor) => VisitList(GenericArguments, visitor);
        public override string ToString() => "xml!!" + XmlNamespace + ":" + Name;
    }
}