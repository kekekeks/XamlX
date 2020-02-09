using System.Collections.Generic;
using System.Linq;
using Visitor = XamlX.Ast.IXamlAstVisitor;
namespace XamlX.Ast
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstXmlTypeReference : XamlAstNode, IXamlAstTypeReference
    {
        public string XmlNamespace { get; set; }
        public string Name { get; set; }
        public bool IsMarkupExtension { get; set; }

        public bool Equals(IXamlAstTypeReference other) =>
            other is XamlAstXmlTypeReference xml
            && xml.Name == Name && xml.XmlNamespace == XmlNamespace
            && xml.IsMarkupExtension == IsMarkupExtension;

        public List<XamlAstXmlTypeReference> GenericArguments { get; set; } = new List<XamlAstXmlTypeReference>();

        public XamlAstXmlTypeReference(IXamlLineInfo lineInfo, string xmlNamespace, string name,
            IEnumerable<XamlAstXmlTypeReference> genericArguments = null) : base(lineInfo)
        {
            XmlNamespace = xmlNamespace;
            Name = name;
            GenericArguments = genericArguments?.ToList() ?? GenericArguments;
        }

        public override void VisitChildren(Visitor visitor) => VisitList(GenericArguments, visitor);
        public override string ToString() => "xml!!" + XmlNamespace + ":" + Name;
    }
}
