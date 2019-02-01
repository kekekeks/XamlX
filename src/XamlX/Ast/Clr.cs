using System.Collections.Generic;
using System.Linq;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.XamlXAstVisitorDelegate;

namespace XamlX.Ast
{
    public class XamlXAstClrTypeReference : XamlXAstNode, IXamlXAstTypeReference
    {
        public IXamlXType Type { get; }

        public XamlXAstClrTypeReference(IXamlXLineInfo lineInfo, IXamlXType type) : base(lineInfo)
        {
            Type = type;
        }

        public override string ToString() => Type.GetFqn();
    }

    public class XamlXAstClrPropertyReference : XamlXAstNode, IXamlXAstPropertyReference
    {
        public IXamlXProperty Property { get; set; }

        public XamlXAstClrPropertyReference(IXamlXLineInfo lineInfo, IXamlXProperty property) : base(lineInfo)
        {
            Property = property;
        }

        public override string ToString() => Property.PropertyType.GetFqn() + "." + Property.Name;
    }

    public class XamlXPropertyAssignmentNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXProperty Property { get; set; }
        public IXamlXAstValueNode Value { get; set; }

        public XamlXPropertyAssignmentNode(IXamlXLineInfo lineInfo,
            IXamlXProperty property, IXamlXAstValueNode value)
            : base(lineInfo)
        {
            Property = property;
            Value = value;
        }
    }
    
    public class XamlXPropertyValueManipulationNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXProperty Property { get; set; }
        public IXamlXAstManipulationNode Manipulation { get; set; }
        public XamlXPropertyValueManipulationNode(IXamlXLineInfo lineInfo, 
            IXamlXProperty property, IXamlXAstManipulationNode manipulation) 
            : base(lineInfo)
        {
            Property = property;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Manipulation = (IXamlXAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public class XamlXInstanceMethodCallNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXMethod Method { get; set; }
        public List<IXamlXAstValueNode> Arguments { get; set; }
        public XamlXInstanceMethodCallNode(IXamlXLineInfo lineInfo, 
            IXamlXMethod method, IEnumerable<IXamlXAstValueNode> args) 
            : base(lineInfo)
        {
            Method = method;
            Arguments = args.ToList();
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            VisitList(Arguments, visitor);
        }
    }

    public class XamlXManipulationGroupNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public List<IXamlXAstManipulationNode> Children { get; set; } = new List<IXamlXAstManipulationNode>();
        public XamlXManipulationGroupNode(IXamlXLineInfo lineInfo) : base(lineInfo)
        {
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor) => VisitList(Children, visitor);
    }
}