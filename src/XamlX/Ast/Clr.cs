using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.XamlXAstVisitorDelegate;

namespace XamlX.Ast
{
    public class XamlAstClrTypeReference : XamlAstNode, IXamlAstTypeReference
    {
        public IXamlType Type { get; }

        public XamlAstClrTypeReference(IXamlLineInfo lineInfo, IXamlType type) : base(lineInfo)
        {
            Type = type;
        }

        public override string ToString() => Type.GetFqn();
    }

    public class XamlAstClrPropertyReference : XamlAstNode, IXamlAstPropertyReference
    {
        public IXamlProperty Property { get; set; }

        public XamlAstClrPropertyReference(IXamlLineInfo lineInfo, IXamlProperty property) : base(lineInfo)
        {
            Property = property;
        }

        public override string ToString() => Property.PropertyType.GetFqn() + "." + Property.Name;
    }

    public class XamlPropertyAssignmentNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlProperty Property { get; set; }
        public IXamlAstValueNode Value { get; set; }

        public XamlPropertyAssignmentNode(IXamlLineInfo lineInfo,
            IXamlProperty property, IXamlAstValueNode value)
            : base(lineInfo)
        {
            Property = property;
            Value = value;
        }
    }
    
    public class XamlPropertyValueManipulationNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlProperty Property { get; set; }
        public IXamlAstManipulationNode Manipulation { get; set; }
        public XamlPropertyValueManipulationNode(IXamlLineInfo lineInfo, 
            IXamlProperty property, IXamlAstManipulationNode manipulation) 
            : base(lineInfo)
        {
            Property = property;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Manipulation = (IXamlAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public abstract class XamlXInstanceMethodCallBaseNode : XamlAstNode
    {
        public IXamlMethod Method { get; set; }
        public List<IXamlAstValueNode> Arguments { get; set; }
        public XamlXInstanceMethodCallBaseNode(IXamlLineInfo lineInfo, 
            IXamlMethod method, IEnumerable<IXamlAstValueNode> args) 
            : base(lineInfo)
        {
            Method = method;
            Arguments = args?.ToList() ?? new List<IXamlAstValueNode>();
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            VisitList(Arguments, visitor);
        }
    }
    
    public class XamlXInstanceNoReturnMethodCallNode : XamlXInstanceMethodCallBaseNode, IXamlAstManipulationNode
    {
        public XamlXInstanceNoReturnMethodCallNode(IXamlLineInfo lineInfo, IXamlMethod method, IEnumerable<IXamlAstValueNode> args)
            : base(lineInfo, method, args)
        {
        }
    }
    
    public class XamlXStaticReturnMethodCallNode : XamlXInstanceMethodCallBaseNode, IXamlAstValueNode
    {
        public XamlXStaticReturnMethodCallNode(IXamlLineInfo lineInfo, IXamlMethod method, IEnumerable<IXamlAstValueNode> args)
            : base(lineInfo, method, args)
        {
            Type = new XamlAstClrTypeReference(lineInfo, method.ReturnType);
        }

        public IXamlAstTypeReference Type { get; }
    }

    public class XamlManipulationGroupNode : XamlAstNode, IXamlAstManipulationNode
    {
        public List<IXamlAstManipulationNode> Children { get; set; } = new List<IXamlAstManipulationNode>();
        public XamlManipulationGroupNode(IXamlLineInfo lineInfo) : base(lineInfo)
        {
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor) => VisitList(Children, visitor);
    }

    public class XamlValueWithManipulationNode : XamlAstNode, IXamlAstValueNode
    {
        public IXamlAstValueNode Value { get; set; }
        public IXamlAstManipulationNode Manipulation { get; set; }
        public IXamlAstTypeReference Type => Value.Type;
        
        public XamlValueWithManipulationNode(IXamlLineInfo lineInfo,
            IXamlAstValueNode value,
            IXamlAstManipulationNode manipulation) : base(lineInfo)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlAstValueNode) Value?.Visit(visitor);
            Manipulation = (IXamlAstManipulationNode) Manipulation?.Visit(visitor);
        }
    }

    public class XamlAstNewClrObjectNode : XamlAstNode, IXamlAstValueNode
    {
        public XamlAstNewClrObjectNode(IXamlLineInfo lineInfo,
            IXamlAstTypeReference type,
            List<IXamlAstValueNode> arguments) : base(lineInfo)
        {
            Type = type;
            Arguments = arguments;
        }

        public IXamlAstTypeReference Type { get; set; }
        public List<IXamlAstValueNode> Arguments { get; set; } = new List<IXamlAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
        }
    }

    public class XamlMarkupExtensionNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlAstValueNode Value { get; set; }
        public IXamlProperty Property { get; set; }
        public IXamlMethod ProvideValue { get; }
        public IXamlMethod Manipulation { get; set; }

        public XamlMarkupExtensionNode(IXamlLineInfo lineInfo, IXamlProperty property, IXamlMethod provideValue,
            IXamlAstValueNode value, IXamlMethod manipulation) : base(lineInfo)
        {
            Property = property;
            ProvideValue = provideValue;
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
        }
    }
   
}