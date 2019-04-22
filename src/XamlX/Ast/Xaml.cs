using System.Collections.Generic;
using System.Linq;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlXAstVisitor;
namespace XamlX.Ast
{

    public class XamlXAstXmlDirective : XamlXAstNode, IXamlXAstManipulationNode
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public List<IXamlXAstValueNode> Values { get; set; }

        public XamlXAstXmlDirective(IXamlXLineInfo lineInfo,
            string ns, string name, IEnumerable<IXamlXAstValueNode> values) : base(lineInfo)
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

    public class XamlXAstXamlPropertyValueNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXAstPropertyReference Property { get; set; }
        public List<IXamlXAstValueNode> Values { get; set; }

        public XamlXAstXamlPropertyValueNode(IXamlXLineInfo lineInfo,
            IXamlXAstPropertyReference property, IXamlXAstValueNode value) : base(lineInfo)
        {
            Property = property;
            Values = new List<IXamlXAstValueNode> {value};
        }
        
        public XamlXAstXamlPropertyValueNode(IXamlXLineInfo lineInfo,
            IXamlXAstPropertyReference property, IEnumerable<IXamlXAstValueNode> values) : base(lineInfo)
        {
            Property = property;
            Values = values.ToList();
        }

        public override void VisitChildren(Visitor visitor)
        {
            Property = (IXamlXAstPropertyReference) Property.Visit(visitor);
            VisitList(Values, visitor);
        }
    }

    public class XamlXAstObjectNode : XamlXAstNode, IXamlXAstValueNode
    {
        public XamlXAstObjectNode(IXamlXLineInfo lineInfo, IXamlXAstTypeReference type) : base(lineInfo)
        {
            Type = type;
        }

        public IXamlXAstTypeReference Type { get; set; }
        public List<IXamlXAstNode> Children { get; set; } = new List<IXamlXAstNode>();
        public List<IXamlXAstValueNode> Arguments { get; set; } = new List<IXamlXAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlXAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
            VisitList(Children, visitor);
        }
    }
    
    

    public class XamlXAstTextNode : XamlXAstNode, IXamlXAstValueNode
    {
        public string Text { get; set; }

        public XamlXAstTextNode(IXamlXLineInfo lineInfo, string text, IXamlXType type = null) : base(lineInfo)
        {
            Text = text;
            if (type != null)
                Type = new XamlXAstClrTypeReference(lineInfo, type, false);
            else
                Type = new XamlXAstXmlTypeReference(lineInfo, XamlNamespaces.Xaml2006, "String");
        }

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlXAstTypeReference) Type.Visit(visitor);
        }

        public IXamlXAstTypeReference Type { get; set; }
    }
    
    public class XamlXAstNamePropertyReference : XamlXAstNode, IXamlXAstPropertyReference
    {
        public IXamlXAstTypeReference DeclaringType { get; set; }
        public string Name { get; set; }
        public IXamlXAstTypeReference TargetType { get; set; }

        public XamlXAstNamePropertyReference(IXamlXLineInfo lineInfo,
            IXamlXAstTypeReference declaringType, string name, IXamlXAstTypeReference targetType) : base(lineInfo)
        {
            DeclaringType = declaringType;
            Name = name;
            TargetType = targetType;
        }

        public override void VisitChildren(Visitor visitor)
        {
            DeclaringType = (IXamlXAstTypeReference) DeclaringType.Visit(visitor);
            TargetType = (IXamlXAstTypeReference) TargetType.Visit(visitor);
        }
    }
}
