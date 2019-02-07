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
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            context.LdLocal(this, codeGen);
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

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            var rv = context.Emit(Value, codeGen, Local.Type);
            codeGen.Generator.Emit(OpCodes.Dup);
            context.StLocal(Local, codeGen);
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

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            // Discard the stack value we are "supposed" to manipulate
            codeGen.Generator.Emit(OpCodes.Pop);
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

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            context.Emit(Manipulation, codeGen, null);
            return XamlIlNodeEmitResult.Void;
        }
    }

}