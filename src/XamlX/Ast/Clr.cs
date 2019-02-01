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

    public class XamlXInstanceMethodCallNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlMethod Method { get; set; }
        public List<IXamlAstValueNode> Arguments { get; set; }
        public XamlXInstanceMethodCallNode(IXamlLineInfo lineInfo, 
            IXamlMethod method, IEnumerable<IXamlAstValueNode> args) 
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

    public class XamlManipulationGroupNode : XamlAstNode, IXamlAstManipulationNode
    {
        public List<IXamlAstManipulationNode> Children { get; set; } = new List<IXamlAstManipulationNode>();
        public XamlManipulationGroupNode(IXamlLineInfo lineInfo) : base(lineInfo)
        {
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor) => VisitList(Children, visitor);
    }

    public class XamlXNullDirectiveNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode
    {
        public XamlXNullDirectiveNode(IXamlLineInfo lineInfo) : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(lineInfo, XamlXNullType.Instance);
        }

        public IXamlAstTypeReference Type { get; }
        public void Emit(XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            codeGen.Generator.Emit(OpCodes.Ldnull);
        }
    }

    public class XamlXTypeDirectiveNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode
    {
        private readonly IXamlType _systemType;

        public XamlXTypeDirectiveNode(IXamlLineInfo lineInfo, IXamlAstTypeReference value,
            IXamlType systemType) : base(lineInfo)
        {
            _systemType = systemType;
            Type = new XamlAstClrTypeReference(this, systemType);
            Value = value;
        }

        public IXamlAstTypeReference Type { get; }
        public IXamlAstTypeReference Value { get; set; }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = visitor(Value) as IXamlAstTypeReference;
        }

        public void Emit(XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            var type = Value.GetClrType();
            var method = _systemType.Methods.FirstOrDefault(m =>
                m.Name == "GetTypeFromHandle" && m.Parameters.Count == 1 &&
                m.Parameters[0].Name == "RuntimeTypeHandle");

            if (method == null)
                throw new XamlTypeSystemException(
                    $"Unable to find GetTypeFromHandle(RuntimeTypeHandle) on {_systemType.GetFqn()}");
            codeGen.Generator
                .Emit(OpCodes.Ldtoken, type)
                .Emit(OpCodes.Call, method);
        }
    }
}