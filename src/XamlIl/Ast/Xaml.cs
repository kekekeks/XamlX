using System.Collections.Generic;
using System.Linq;
using Visitor = XamlIl.Ast.XamlIlAstVisitorDelegate;

namespace XamlIl.Ast
{

    public class XamlIlAstXmlDirective : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public List<IXamlIlAstValueNode> Values { get; set; }

        public XamlIlAstXmlDirective(IXamlIlLineInfo lineInfo,
            string ns, string name, IEnumerable<IXamlIlAstValueNode> values) : base(lineInfo)
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

    public class XamlIlAstMarkupExtensionNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public IXamlIlAstTypeReference Type { get; set; }
        public List<IXamlIlAstNode> ConstructorArguments { get; set; } = new List<IXamlIlAstNode>();

        public List<XamlIlAstXamlPropertyValueNode> Properties { get; set; } =
            new List<XamlIlAstXamlPropertyValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlIlAstTypeReference) Type.Visit(visitor);

        }

        public XamlIlAstMarkupExtensionNode(IXamlIlLineInfo lineInfo) : base(lineInfo)
        {
        }
    }


    public class XamlIlAstXamlPropertyValueNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public IXamlIlAstPropertyReference Property { get; set; }
        public List<IXamlIlAstValueNode> Values { get; set; }

        public XamlIlAstXamlPropertyValueNode(IXamlIlLineInfo lineInfo,
            IXamlIlAstPropertyReference property, IXamlIlAstValueNode value) : base(lineInfo)
        {
            Property = property;
            Values = new List<IXamlIlAstValueNode> {value};
        }
        
        public XamlIlAstXamlPropertyValueNode(IXamlIlLineInfo lineInfo,
            IXamlIlAstPropertyReference property, IEnumerable<IXamlIlAstValueNode> values) : base(lineInfo)
        {
            Property = property;
            Values = values.ToList();
        }

        public override void VisitChildren(Visitor visitor)
        {
            Property = (IXamlIlAstPropertyReference) Property.Visit(visitor);
            VisitList(Values, visitor);
        }
    }

    public class XamlIlAstNewInstanceNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public XamlIlAstNewInstanceNode(IXamlIlLineInfo lineInfo, IXamlIlAstTypeReference type) : base(lineInfo)
        {
            Type = type;
        }

        public IXamlIlAstTypeReference Type { get; set; }
        public List<IXamlIlAstNode> Children { get; set; } = new List<IXamlIlAstNode>();
        public List<IXamlIlAstValueNode> Arguments { get; set; } = new List<IXamlIlAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlIlAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
            VisitList(Children, visitor);
        }
    }

    public class XamlIlAstRootInstanceNode : XamlIlAstNewInstanceNode
    {
        public Dictionary<string, string> XmlNamespaces { get; set; } = new Dictionary<string, string>();

        public XamlIlAstRootInstanceNode(IXamlIlLineInfo lineInfo, IXamlIlAstTypeReference type)
            : base(lineInfo, type)
        {
        }
    }

    public class XamlIlAstTextNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public string Text { get; set; }

        public XamlIlAstTextNode(IXamlIlLineInfo lineInfo, string text) : base(lineInfo)
        {
            Text = text;
            Type = new XamlIlAstXmlTypeReference(lineInfo, XamlNamespaces.Xaml2006, "String");
        }

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlIlAstTypeReference) Type.Visit(visitor);
        }

        public IXamlIlAstTypeReference Type { get; set; }
    }
    
    public class XamlIlAstNamePropertyReference : XamlIlAstNode, IXamlIlAstPropertyReference
    {
        public IXamlIlAstTypeReference DeclaringType { get; set; }
        public string Name { get; set; }
        public IXamlIlAstTypeReference TargetType { get; set; }

        public XamlIlAstNamePropertyReference(IXamlIlLineInfo lineInfo,
            IXamlIlAstTypeReference declaringType, string name, IXamlIlAstTypeReference targetType) : base(lineInfo)
        {
            DeclaringType = declaringType;
            Name = name;
            TargetType = targetType;
        }

        public override void VisitChildren(Visitor visitor)
        {
            DeclaringType = (IXamlIlAstTypeReference) DeclaringType.Visit(visitor);
            TargetType = (IXamlIlAstTypeReference) TargetType.Visit(visitor);
        }
    }
}