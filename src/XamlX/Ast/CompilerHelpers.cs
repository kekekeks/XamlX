using System.Reflection.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.Ast
{
    public class XamlAstCompilerLocalNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode
    {
        public IXamlType Type { get; }
        public XamlAstCompilerLocalNode(IXamlLineInfo lineInfo, IXamlType type) : base(lineInfo)
        {
            Type = type;
        }

        IXamlAstTypeReference IXamlAstValueNode.Type => new XamlAstClrTypeReference(this, Type);
        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            context.LdLocal(this);
            return XamlNodeEmitResult.Type(Type);
        }
    }

    public class XamlAstLocalInitializationNodeEmitter : XamlValueWithSideEffectNodeBase, IXamlAstEmitableNode
    {
        public XamlAstCompilerLocalNode Local { get; set; }

        public XamlAstLocalInitializationNodeEmitter(IXamlLineInfo lineInfo,
            IXamlAstValueNode value,
            XamlAstCompilerLocalNode local) : base(lineInfo, value)
        {
            Value = value;
            Local = local;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            base.VisitChildren(visitor);
            Local = (XamlAstCompilerLocalNode) Local.Visit(visitor);
        }

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            var rv = context.Emit(Value, codeGen, Local.Type);
            codeGen.Emit(OpCodes.Dup);
            context.StLocal(Local);
            return rv;
        }
    }

    public class XamlAstManipulationImperativeNode : XamlAstNode, IXamlAstManipulationNode, IXamlAstEmitableNode
    {
        public IXamlAstImperativeNode Imperative { get; set; }

        public XamlAstManipulationImperativeNode(IXamlLineInfo lineInfo, IXamlAstImperativeNode imperative) 
            : base(lineInfo)
        {
            Imperative = imperative;
        }

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            // Discard the stack value we are "supposed" to manipulate
            codeGen.Emit(OpCodes.Pop);
            context.Emit(Imperative, codeGen, null);
            return XamlNodeEmitResult.Void;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Imperative = (IXamlAstImperativeNode)Imperative.Visit(visitor);
        }
    }

    public class XamlAstImperativeValueManipulation : XamlAstNode, IXamlAstImperativeNode, IXamlAstEmitableNode
    {
        public IXamlAstValueNode Value { get; set; }
        public IXamlAstManipulationNode Manipulation { get; set; }

        public XamlAstImperativeValueManipulation(IXamlLineInfo lineInfo, 
            IXamlAstValueNode value, IXamlAstManipulationNode manipulation) : base(lineInfo)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (XamlAstCompilerLocalNode) Value.Visit(visitor);
            Manipulation = (IXamlAstManipulationNode) Manipulation.Visit(visitor);
        }

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            context.Emit(Manipulation, codeGen, null);
            return XamlNodeEmitResult.Void;
        }
    }

    public class XamlAstContextLocalNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode
    {
        public XamlAstContextLocalNode(IXamlLineInfo lineInfo, IXamlType type) : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(this, type);
        }

        public IXamlAstTypeReference Type { get; }
        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            codeGen.Ldloc(context.ContextLocal);
            return XamlNodeEmitResult.Type(Type.GetClrType());
        }
    }

    public class XamlAstRuntimeCastNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode
    {
        public XamlAstRuntimeCastNode(IXamlLineInfo lineInfo, IXamlAstValueNode value, IXamlAstTypeReference castTo) : base(lineInfo)
        {
            Value = value;
            Type = castTo;
        }
        public IXamlAstValueNode Value { get; set; }
        public IXamlAstTypeReference Type { get; set; }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
            Type = (IXamlAstTypeReference) Type.Visit(visitor);
        }

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            context.Emit(Value, codeGen, context.Configuration.WellKnownTypes.Object);
            var t = Type.GetClrType();
            if (t.IsValueType)
                codeGen.Unbox_Any(t);
            else
                codeGen.Castclass(t);            
            return XamlNodeEmitResult.Type(t);
        }
    }

}