using System.Reflection.Emit;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace XamlIl.Ast
{
    public class XamlIlAstCompilerLocalNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public IXamlIlType Type { get; }
        public XamlIlAstCompilerLocalNode(IXamlIlLineInfo lineInfo, IXamlIlType type) : base(lineInfo)
        {
            Type = type;
        }

        IXamlIlAstTypeReference IXamlIlAstValueNode.Type => new XamlIlAstClrTypeReference(this, Type);
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            context.LdLocal(this);
            return XamlIlNodeEmitResult.Type(Type);
        }
    }

    public class XamlIlAstLocalInitializationNodeEmitter : XamlIlValueWithSideEffectNodeBase, IXamlIlAstEmitableNode
    {
        public XamlIlAstCompilerLocalNode Local { get; set; }

        public XamlIlAstLocalInitializationNodeEmitter(IXamlIlLineInfo lineInfo,
            IXamlIlAstValueNode value,
            XamlIlAstCompilerLocalNode local) : base(lineInfo, value)
        {
            Value = value;
            Local = local;
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            base.VisitChildren(visitor);
            Local = (XamlIlAstCompilerLocalNode) Local.Visit(visitor);
        }

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            var rv = context.Emit(Value, codeGen, Local.Type);
            codeGen.Emit(OpCodes.Dup);
            context.StLocal(Local);
            return rv;
        }
    }

    public class XamlIlAstManipulationImperativeNode : XamlIlAstNode, IXamlIlAstManipulationNode, IXamlIlAstEmitableNode
    {
        public IXamlIlAstImperativeNode Imperative { get; set; }

        public XamlIlAstManipulationImperativeNode(IXamlIlLineInfo lineInfo, IXamlIlAstImperativeNode imperative) 
            : base(lineInfo)
        {
            Imperative = imperative;
        }

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            // Discard the stack value we are "supposed" to manipulate
            codeGen.Emit(OpCodes.Pop);
            context.Emit(Imperative, codeGen, null);
            return XamlIlNodeEmitResult.Void;
        }
    }

    public class XamlIlAstImperativeValueManipulation : XamlIlAstNode, IXamlIlAstImperativeNode, IXamlIlAstEmitableNode
    {
        public IXamlIlAstValueNode Value { get; set; }
        public IXamlIlAstManipulationNode Manipulation { get; set; }

        public XamlIlAstImperativeValueManipulation(IXamlIlLineInfo lineInfo, 
            IXamlIlAstValueNode value, IXamlIlAstManipulationNode manipulation) : base(lineInfo)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            Value = (XamlIlAstCompilerLocalNode) Value.Visit(visitor);
            Manipulation = (IXamlIlAstManipulationNode) Manipulation.Visit(visitor);
        }

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            context.Emit(Manipulation, codeGen, null);
            return XamlIlNodeEmitResult.Void;
        }
    }

    public class XamlIlAstContextLocalNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlAstContextLocalNode(IXamlIlLineInfo lineInfo, IXamlIlType type) : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(this, type);
        }

        public IXamlIlAstTypeReference Type { get; }
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            codeGen.Ldloc(context.ContextLocal);
            return XamlIlNodeEmitResult.Type(Type.GetClrType());
        }
    }

    public class XamlIlAstRuntimeCastNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlAstRuntimeCastNode(IXamlIlLineInfo lineInfo, IXamlIlAstValueNode value, IXamlIlAstTypeReference castTo) : base(lineInfo)
        {
            Value = value;
            Type = castTo;
        }
        public IXamlIlAstValueNode Value { get; set; }
        public IXamlIlAstTypeReference Type { get; set; }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            Value = (IXamlIlAstValueNode) Value.Visit(visitor);
            Type = (IXamlIlAstTypeReference) Type.Visit(visitor);
        }

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            context.Emit(Value, codeGen, context.Configuration.WellKnownTypes.Object);
            var t = Type.GetClrType();
            if (t.IsValueType)
                codeGen.Unbox_Any(t);
            else
                codeGen.Castclass(t);            
            return XamlIlNodeEmitResult.Type(t);
        }
    }

}