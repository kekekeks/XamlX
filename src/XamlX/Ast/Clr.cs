using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Transform;
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

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
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

    public abstract class XamlXInstanceMethodCallBaseNode : XamlXAstNode
    {
        public IXamlXMethod Method { get; set; }
        public List<IXamlXAstValueNode> Arguments { get; set; }
        public XamlXInstanceMethodCallBaseNode(IXamlXLineInfo lineInfo, 
            IXamlXMethod method, IEnumerable<IXamlXAstValueNode> args) 
            : base(lineInfo)
        {
            Method = method;
            Arguments = args?.ToList() ?? new List<IXamlXAstValueNode>();
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            VisitList(Arguments, visitor);
        }
    }
    
    public class XamlXInstanceNoReturnMethodCallNode : XamlXInstanceMethodCallBaseNode, IXamlXAstManipulationNode
    {
        public XamlXInstanceNoReturnMethodCallNode(IXamlXLineInfo lineInfo, IXamlXMethod method, IEnumerable<IXamlXAstValueNode> args)
            : base(lineInfo, method, args)
        {
        }
    }
    
    public class XamlXStaticReturnMethodCallNode : XamlXInstanceMethodCallBaseNode, IXamlXAstValueNode
    {
        public XamlXStaticReturnMethodCallNode(IXamlXLineInfo lineInfo, IXamlXMethod method, IEnumerable<IXamlXAstValueNode> args)
            : base(lineInfo, method, args)
        {
            Type = new XamlXAstClrTypeReference(lineInfo, method.ReturnType);
        }

        public IXamlXAstTypeReference Type { get; }
    }

    public class XamlXManipulationGroupNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public List<IXamlXAstManipulationNode> Children { get; set; } = new List<IXamlXAstManipulationNode>();
        public XamlXManipulationGroupNode(IXamlXLineInfo lineInfo) : base(lineInfo)
        {
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor) => VisitList(Children, visitor);
    }

    public abstract class XamlXValueWithSideEffectNodeBase : XamlXAstNode, IXamlXAstValueNode
    {
        protected XamlXValueWithSideEffectNodeBase(IXamlXLineInfo lineInfo, IXamlXAstValueNode value) : base(lineInfo)
        {
            Value = value;
        }

        public IXamlXAstValueNode Value { get; set; }
        public virtual IXamlXAstTypeReference Type => Value.Type;

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
        }
    }
    
    public class XamlXValueWithManipulationNode : XamlXValueWithSideEffectNodeBase
    {
        public IXamlXAstManipulationNode Manipulation { get; set; }

        public XamlXValueWithManipulationNode(IXamlXLineInfo lineInfo,
            IXamlXAstValueNode value,
            IXamlXAstManipulationNode manipulation) : base(lineInfo, value)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            base.VisitChildren(visitor);
            Manipulation = (IXamlXAstManipulationNode) Manipulation?.Visit(visitor);
        }
    }

    public class XamlXAstNewClrObjectNode : XamlXAstNode, IXamlXAstValueNode
    {
        public XamlXAstNewClrObjectNode(IXamlXLineInfo lineInfo,
            IXamlXAstTypeReference type,
            List<IXamlXAstValueNode> arguments) : base(lineInfo)
        {
            Type = type;
            Arguments = arguments;
        }

        public IXamlXAstTypeReference Type { get; set; }
        public List<IXamlXAstValueNode> Arguments { get; set; } = new List<IXamlXAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlXAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
        }
    }

    public class XamlXMarkupExtensionNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXAstValueNode Value { get; set; }
        public IXamlXProperty Property { get; set; }
        public IXamlXMethod ProvideValue { get; }
        public IXamlXMethod Manipulation { get; set; }

        public XamlXMarkupExtensionNode(IXamlXLineInfo lineInfo, IXamlXProperty property, IXamlXMethod provideValue,
            IXamlXAstValueNode value, IXamlXMethod manipulation) : base(lineInfo)
        {
            Property = property;
            ProvideValue = provideValue;
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
        }
    }
    
    public class XamlXObjectInitializationNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXAstManipulationNode Manipulation { get; set; }
        public IXamlXType Type { get; set; }
        public XamlXObjectInitializationNode(IXamlXLineInfo lineInfo, 
            IXamlXAstManipulationNode manipulation, IXamlXType type) 
            : base(lineInfo)
        {
            Manipulation = manipulation;
            Type = type;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Manipulation = (IXamlXAstManipulationNode) Manipulation.Visit(visitor);
        }
    }
}