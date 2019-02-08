using System.Reflection.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.Ast
{
    public class XamlXAstCompilerLocalNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public IXamlXType Type { get; }
        public XamlXAstCompilerLocalNode(IXamlXLineInfo lineInfo, IXamlXType type) : base(lineInfo)
        {
            Type = type;
        }

        IXamlXAstTypeReference IXamlXAstValueNode.Type => new XamlXAstClrTypeReference(this, Type);
        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            context.LdLocal(this, codeGen);
            return XamlXNodeEmitResult.Type(Type);
        }
    }

    public class XamlXAstLocalInitializationNodeEmitter : XamlXValueWithSideEffectNodeBase, IXamlXAstEmitableNode
    {
        public XamlXAstCompilerLocalNode Local { get; set; }

        public XamlXAstLocalInitializationNodeEmitter(IXamlXLineInfo lineInfo,
            IXamlXAstValueNode value,
            XamlXAstCompilerLocalNode local) : base(lineInfo, value)
        {
            Value = value;
            Local = local;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            base.VisitChildren(visitor);
            Local = (XamlXAstCompilerLocalNode) Local.Visit(visitor);
        }

        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            var rv = context.Emit(Value, codeGen, Local.Type);
            codeGen.Generator.Emit(OpCodes.Dup);
            context.StLocal(Local, codeGen);
            return rv;
        }
    }

    public class XamlXAstManipulationImperativeNode : XamlXAstNode, IXamlXAstManipulationNode, IXamlXAstEmitableNode
    {
        public IXamlXAstImperativeNode Imperative { get; set; }

        public XamlXAstManipulationImperativeNode(IXamlXLineInfo lineInfo, IXamlXAstImperativeNode imperative) 
            : base(lineInfo)
        {
            Imperative = imperative;
        }

        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            // Discard the stack value we are "supposed" to manipulate
            codeGen.Generator.Emit(OpCodes.Pop);
            context.Emit(Imperative, codeGen, null);
            return XamlXNodeEmitResult.Void;
        }
    }

    public class XamlXAstImperativeValueManipulation : XamlXAstNode, IXamlXAstImperativeNode, IXamlXAstEmitableNode
    {
        public IXamlXAstValueNode Value { get; set; }
        public IXamlXAstManipulationNode Manipulation { get; set; }

        public XamlXAstImperativeValueManipulation(IXamlXLineInfo lineInfo, 
            IXamlXAstValueNode value, IXamlXAstManipulationNode manipulation) : base(lineInfo)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (XamlXAstCompilerLocalNode) Value.Visit(visitor);
            Manipulation = (IXamlXAstManipulationNode) Manipulation.Visit(visitor);
        }

        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            context.Emit(Manipulation, codeGen, null);
            return XamlXNodeEmitResult.Void;
        }
    }

    public class XamlXAstContextLocalNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public XamlXAstContextLocalNode(IXamlXLineInfo lineInfo, IXamlXType type) : base(lineInfo)
        {
            Type = new XamlXAstClrTypeReference(this, type);
        }

        public IXamlXAstTypeReference Type { get; }
        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            codeGen.Generator.Ldloc(context.ContextLocal);
            return XamlXNodeEmitResult.Type(Type.GetClrType());
        }
    }

    public class XamlXAstRuntimeCastNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public XamlXAstRuntimeCastNode(IXamlXLineInfo lineInfo, IXamlXAstValueNode value, IXamlXAstTypeReference castTo) : base(lineInfo)
        {
            Value = value;
            Type = castTo;
        }
        public IXamlXAstValueNode Value { get; set; }
        public IXamlXAstTypeReference Type { get; set; }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
            Type = (IXamlXAstTypeReference) Type.Visit(visitor);
        }

        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            context.Emit(Value, codeGen, context.Configuration.WellKnownTypes.Object);
            var t = Type.GetClrType();
            codeGen.Generator.Emit(OpCodes.Castclass, t);
            return XamlXNodeEmitResult.Type(t);
        }
    }

}