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

    public class XamlXNullDirectiveNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public XamlXNullDirectiveNode(IXamlXLineInfo lineInfo) : base(lineInfo)
        {
            Type = new XamlXAstClrTypeReference(lineInfo, XamlXNullType.Instance);
        }

        public IXamlXAstTypeReference Type { get; }
        public void Emit(XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            codeGen.Generator.Emit(OpCodes.Ldnull);
        }
    }

    public class XamlXTypeDirectiveNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        private readonly IXamlXType _systemType;

        public XamlXTypeDirectiveNode(IXamlXLineInfo lineInfo, IXamlXAstTypeReference value,
            IXamlXType systemType) : base(lineInfo)
        {
            _systemType = systemType;
            Type = new XamlXAstClrTypeReference(this, systemType);
            Value = value;
        }

        public IXamlXAstTypeReference Type { get; }
        public IXamlXAstTypeReference Value { get; set; }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = visitor(Value) as IXamlXAstTypeReference;
        }

        public void Emit(XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            var type = Value.GetClrType();
            var method = _systemType.Methods.FirstOrDefault(m =>
                m.Name == "GetTypeFromHandle" && m.Parameters.Count == 1 &&
                m.Parameters[0].Name == "RuntimeTypeHandle");

            if (method == null)
                throw new XamlXTypeSystemException(
                    $"Unable to find GetTypeFromHandle(RuntimeTypeHandle) on {_systemType.GetFqn()}");
            codeGen.Generator
                .Emit(OpCodes.Ldtoken, type)
                .Emit(OpCodes.Call, method);
        }
    }
}