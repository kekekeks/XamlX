using System.Collections.Generic;
using System.Linq;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlAstVisitor;
namespace XamlX.Ast
{

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstXmlDirective : XamlAstNode, IXamlAstManipulationNode
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public List<IXamlAstValueNode> Values { get; set; }

        public XamlAstXmlDirective(IXamlLineInfo lineInfo,
            string ns, string name, IEnumerable<IXamlAstValueNode> values) : base(lineInfo)
        {
            Namespace = ns;
            Name = name;
            Values = values.ToList();
        }

        public override void VisitChildren(Visitor visitor)
        {
            VisitList(Values, visitor);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstXamlPropertyValueNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlAstPropertyReference Property { get; set; }
        public List<IXamlAstValueNode> Values { get; set; }

        public XamlAstXamlPropertyValueNode(IXamlLineInfo lineInfo,
            IXamlAstPropertyReference property, IXamlAstValueNode value) : base(lineInfo)
        {
            Property = property;
            Values = new List<IXamlAstValueNode> {value};
        }
        
        public XamlAstXamlPropertyValueNode(IXamlLineInfo lineInfo,
            IXamlAstPropertyReference property, IEnumerable<IXamlAstValueNode> values) : base(lineInfo)
        {
            Property = property;
            Values = values.ToList();
        }

        public override void VisitChildren(Visitor visitor)
        {
            Property = (IXamlAstPropertyReference) Property.Visit(visitor);
            VisitList(Values, visitor);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstObjectNode : XamlAstNode, IXamlAstValueNode
    {
        public XamlAstObjectNode(IXamlLineInfo lineInfo, IXamlAstTypeReference type) : base(lineInfo)
        {
            Type = type;
        }

        public IXamlAstTypeReference Type { get; set; }
        public List<IXamlAstNode> Children { get; set; } = new List<IXamlAstNode>();
        public List<IXamlAstValueNode> Arguments { get; set; } = new List<IXamlAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
            VisitList(Children, visitor);
        }
    }
    
    

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstTextNode : XamlAstNode, IXamlAstValueNode
    {
        public string Text { get; set; }

        public XamlAstTextNode(IXamlLineInfo lineInfo, string text, IXamlType type = null) : base(lineInfo)
        {
            Text = text;
            if (type != null)
                Type = new XamlAstClrTypeReference(lineInfo, type, false);
            else
                Type = new XamlAstXmlTypeReference(lineInfo, XamlNamespaces.Xaml2006, "String");
        }

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlAstTypeReference) Type.Visit(visitor);
        }

        public IXamlAstTypeReference Type { get; set; }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstNamePropertyReference : XamlAstNode, IXamlAstPropertyReference
    {
        public IXamlAstTypeReference DeclaringType { get; set; }
        public string Name { get; set; }
        public IXamlAstTypeReference TargetType { get; set; }

        public XamlAstNamePropertyReference(IXamlLineInfo lineInfo,
            IXamlAstTypeReference declaringType, string name, IXamlAstTypeReference targetType) : base(lineInfo)
        {
            DeclaringType = declaringType;
            Name = name;
            TargetType = targetType;
        }

        public override void VisitChildren(Visitor visitor)
        {
            DeclaringType = (IXamlAstTypeReference) DeclaringType.Visit(visitor);
            TargetType = (IXamlAstTypeReference) TargetType.Visit(visitor);
        }
    }
}
