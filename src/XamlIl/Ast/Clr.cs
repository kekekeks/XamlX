using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.Transform;
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

    public class XamlIlNullDirectiveNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlNullDirectiveNode(IXamlIlLineInfo lineInfo) : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(lineInfo, XamlIlNullType.Instance);
        }

        public IXamlIlAstTypeReference Type { get; }
        public void Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            codeGen.Generator.Emit(OpCodes.Ldnull);
        }
    }

    public class XamlIlTypeDirectiveNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        private readonly IXamlIlType _systemType;

        public XamlIlTypeDirectiveNode(IXamlIlLineInfo lineInfo, IXamlIlAstTypeReference value,
            IXamlIlType systemType) : base(lineInfo)
        {
            _systemType = systemType;
            Type = new XamlIlAstClrTypeReference(this, systemType);
            Value = value;
        }

        public IXamlIlAstTypeReference Type { get; }
        public IXamlIlAstTypeReference Value { get; set; }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            Value = visitor(Value) as IXamlIlAstTypeReference;
        }

        public void Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            var type = Value.GetClrType();
            var method = _systemType.Methods.FirstOrDefault(m =>
                m.Name == "GetTypeFromHandle" && m.Parameters.Count == 1 &&
                m.Parameters[0].Name == "RuntimeTypeHandle");

            if (method == null)
                throw new XamlIlTypeSystemException(
                    $"Unable to find GetTypeFromHandle(RuntimeTypeHandle) on {_systemType.GetFqn()}");
            codeGen.Generator
                .Emit(OpCodes.Ldtoken, type)
                .Emit(OpCodes.Call, method);
        }
    }
}