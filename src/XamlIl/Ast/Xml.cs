using System.Collections.Generic;
using System.Linq;
using Visitor = XamlIl.Ast.IXamlIlAstVisitor;
namespace XamlIl.Ast
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlAstXmlTypeReference : XamlIlAstNode, IXamlIlAstTypeReference
    {
        public string XmlNamespace { get; set; }
        public string Name { get; set; }
        public bool IsMarkupExtension { get; set; }

        public bool Equals(IXamlIlAstTypeReference other) =>
            other is XamlIlAstXmlTypeReference xml
            && xml.Name == Name && xml.XmlNamespace == XmlNamespace
            && xml.IsMarkupExtension == IsMarkupExtension;

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
