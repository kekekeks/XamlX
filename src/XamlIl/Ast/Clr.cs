using System.Collections.Generic;
using System.Linq;
using XamlIl.TypeSystem;
using Visitor = XamlIl.Ast.XamlIlAstVisitorDelegate;

namespace XamlIl.Ast
{
    public class XamlIlAstClrTypeReference : XamlIlAstNode, IXamlIlAstTypeReference
    {
        public IXamlIlType Type { get; }

        public XamlIlAstClrTypeReference(IXamlIlLineInfo lineInfo, IXamlIlType type) : base(lineInfo)
        {
            Type = type;
        }

        public override string ToString() => Type.GetFqn();
    }

    public class XamlIlAstClrPropertyReference : XamlIlAstNode, IXamlIlAstPropertyReference
    {
        public IXamlIlProperty Property { get; set; }

        public XamlIlAstClrPropertyReference(IXamlIlLineInfo lineInfo, IXamlIlProperty property) : base(lineInfo)
        {
            Property = property;
        }

        public override string ToString() => Property.PropertyType.GetFqn() + "." + Property.Name;
    }

    public class XamlIlPropertyAssignmentNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public IXamlIlProperty Property { get; set; }
        public IXamlIlAstValueNode Value { get; set; }

        public XamlIlPropertyAssignmentNode(IXamlIlLineInfo lineInfo,
            IXamlIlProperty property, IXamlIlAstValueNode value)
            : base(lineInfo)
        {
            Property = property;
            Value = value;
        }
    }
    
    public class XamlIlPropertyValueManipulationNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public IXamlIlProperty Property { get; set; }
        public IXamlIlAstManipulationNode Manipulation { get; set; }
        public XamlIlPropertyValueManipulationNode(IXamlIlLineInfo lineInfo, 
            IXamlIlProperty property, IXamlIlAstManipulationNode manipulation) 
            : base(lineInfo)
        {
            Property = property;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            Manipulation = (IXamlIlAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public class XamlIlInstanceMethodCallNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public IXamlIlMethod Method { get; set; }
        public List<IXamlIlAstValueNode> Arguments { get; set; }
        public XamlIlInstanceMethodCallNode(IXamlIlLineInfo lineInfo, 
            IXamlIlMethod method, IEnumerable<IXamlIlAstValueNode> args) 
            : base(lineInfo)
        {
            Method = method;
            Arguments = args.ToList();
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            VisitList(Arguments, visitor);
        }
    }

    public class XamlIlManipulationGroupNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public List<IXamlIlAstManipulationNode> Children { get; set; } = new List<IXamlIlAstManipulationNode>();
        public XamlIlManipulationGroupNode(IXamlIlLineInfo lineInfo) : base(lineInfo)
        {
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor) => VisitList(Children, visitor);
    }
}