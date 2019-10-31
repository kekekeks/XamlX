using System.Reflection.Emit;
using XamlX.Transform;
using XamlX.IL.Emitters;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlAstVisitor;
using XamlX.IL;

namespace XamlX.Ast
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstCompilerLocalNode : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        private XamlAstClrTypeReference _typeReference;
        public IXamlType Type { get; }
        public XamlAstCompilerLocalNode(IXamlLineInfo lineInfo, XamlAstClrTypeReference type) : base(lineInfo)
        {
            Type = type.Type;
            _typeReference = type;
        }

        public XamlAstCompilerLocalNode(IXamlAstValueNode value) : this(value, value.Type.GetClrTypeReference())
        {
            
        }
        
        IXamlAstTypeReference IXamlAstValueNode.Type => _typeReference;
        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            context.LdLocal(this, codeGen);
            return XamlNodeEmitResult.Type(0, Type);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstLocalInitializationNodeEmitter : XamlValueWithSideEffectNodeBase, IXamlAstILEmitableNode
    {
        public XamlAstCompilerLocalNode Local { get; set; }

        public XamlAstLocalInitializationNodeEmitter(IXamlLineInfo lineInfo,
            IXamlAstValueNode value,
            XamlAstCompilerLocalNode local) : base(lineInfo, value)
        {
            Value = value;
            Local = local;
        }

        public override void VisitChildren(Visitor visitor)
        {
            base.VisitChildren(visitor);
            Local = (XamlAstCompilerLocalNode) Local.Visit(visitor);
        }

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            var rv = context.Emit(Value, codeGen, Local.Type);
            codeGen.Emit(OpCodes.Dup);
            context.StLocal(Local, codeGen);
            return XamlNodeEmitResult.Type(0, rv.ReturnType);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlValueNodeWithBeginInit : XamlValueWithSideEffectNodeBase, IXamlAstILEmitableNode
    {
        public XamlValueNodeWithBeginInit(IXamlAstValueNode value) : base(value, value)
        {
        }

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            var res = context.Emit(Value, codeGen, Value.Type.GetClrType());
            var supportInitType = context.Configuration.TypeMappings.SupportInitialize;
            var supportsInitialize = supportInitType != null
                                     && context.Configuration.TypeMappings.SupportInitialize
                                         .IsAssignableFrom(Value.Type.GetClrType());
            if (supportsInitialize)
            {
                codeGen
                    .Dup()
                    .EmitCall(supportInitType.FindMethod(m => m.Name == "BeginInit"));
            }

            return res;
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstManipulationImperativeNode : XamlAstNode, IXamlAstManipulationNode, IXamlAstILEmitableNode
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
            codeGen.Pop();
            context.Emit(Imperative, codeGen, null);
            return XamlNodeEmitResult.Void(1);
        }

        public override void VisitChildren(Visitor visitor)
        {
            Imperative = (IXamlAstImperativeNode)Imperative.Visit(visitor);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstImperativeValueManipulation : XamlAstNode, IXamlAstImperativeNode, IXamlAstILEmitableNode
    {
        public IXamlAstValueNode Value { get; set; }
        public IXamlAstManipulationNode Manipulation { get; set; }

        public XamlAstImperativeValueManipulation(IXamlLineInfo lineInfo, 
            IXamlAstValueNode value, IXamlAstManipulationNode manipulation) : base(lineInfo)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (XamlAstCompilerLocalNode) Value.Visit(visitor);
            Manipulation = (IXamlAstManipulationNode) Manipulation.Visit(visitor);
        }

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            context.Emit(Manipulation, codeGen, null);
            return XamlNodeEmitResult.Void(0);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstContextLocalNode : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        public XamlAstContextLocalNode(IXamlLineInfo lineInfo, IXamlType type) : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(this, type, false);
        }

        public IXamlAstTypeReference Type { get; }
        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            codeGen.Ldloc(context.ContextLocal);
            return XamlNodeEmitResult.Type(0, Type.GetClrType());
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstRuntimeCastNode : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        public XamlAstRuntimeCastNode(IXamlLineInfo lineInfo, IXamlAstValueNode value, IXamlAstTypeReference castTo) : base(lineInfo)
        {
            Value = value;
            Type = castTo;
        }
        public IXamlAstValueNode Value { get; set; }
        public IXamlAstTypeReference Type { get; set; }

        public override void VisitChildren(Visitor visitor)
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
            return XamlNodeEmitResult.Type(0, t);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstNeedsParentStackValueNode : XamlValueWithSideEffectNodeBase,
        IXamlAstILEmitableNode,
        IXamlAstNodeNeedsParentStack
    {
        public XamlAstNeedsParentStackValueNode(IXamlLineInfo lineInfo, IXamlAstValueNode value) : base(lineInfo, value)
        {
        }

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            XamlNeedsParentStackCache.Verify(context, this);
            return context.Emit(Value, codeGen, Value.Type.GetClrType());
        }

        public bool NeedsParentStack => true;
    }

}
