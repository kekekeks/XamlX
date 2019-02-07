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
        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            context.LdLocal(this, codeGen);
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

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            var rv = context.Emit(Value, codeGen, Local.Type);
            codeGen.Generator.Emit(OpCodes.Dup);
            context.StLocal(Local, codeGen);
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

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            // Discard the stack value we are "supposed" to manipulate
            codeGen.Generator.Emit(OpCodes.Pop);
            context.Emit(Imperative, codeGen, null);
            return XamlNodeEmitResult.Void;
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

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            context.Emit(Manipulation, codeGen, null);
            return XamlNodeEmitResult.Void;
        }
    }

}