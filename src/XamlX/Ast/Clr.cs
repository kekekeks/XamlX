using System;
using System.Collections.Generic;
using System.Linq;
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

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
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

    public abstract class XamlMethodCallBaseNode : XamlAstNode
    {
        public IXamlMethod Method { get; set; }
        public List<IXamlAstValueNode> Arguments { get; set; }
        public XamlMethodCallBaseNode(IXamlLineInfo lineInfo, 
            IXamlMethod method, IEnumerable<IXamlAstValueNode> args) 
            : base(lineInfo)
        {
            Method = method;
            Arguments = args?.ToList() ?? new List<IXamlAstValueNode>();
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            VisitList(Arguments, visitor);
        }
    }
    
    public class XamlNoReturnMethodCallNode : XamlMethodCallBaseNode, IXamlAstManipulationNode
    {
        public XamlNoReturnMethodCallNode(IXamlLineInfo lineInfo, IXamlMethod method, IEnumerable<IXamlAstValueNode> args)
            : base(lineInfo, method, args)
        {
        }
    }
    
    public class XamlStaticOrTargetedReturnMethodCallNode : XamlMethodCallBaseNode, IXamlAstValueNode
    {
        public XamlStaticOrTargetedReturnMethodCallNode(IXamlLineInfo lineInfo, IXamlMethod method, IEnumerable<IXamlAstValueNode> args)
            : base(lineInfo, method, args)
        {
            Type = new XamlAstClrTypeReference(lineInfo, method.ReturnType);
        }

        public IXamlAstTypeReference Type { get; }
    }

    public class XamlManipulationGroupNode : XamlAstNode, IXamlAstManipulationNode
    {
        public List<IXamlAstManipulationNode> Children { get; set; } = new List<IXamlAstManipulationNode>();

        public XamlManipulationGroupNode(IXamlLineInfo lineInfo,
            IEnumerable<IXamlAstManipulationNode> children = null)
            : base(lineInfo)
        {
            if (children != null)
                Children.AddRange(children);
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor) => VisitList(Children, visitor);
    }

    public abstract class XamlValueWithSideEffectNodeBase : XamlAstNode, IXamlAstValueNode
    {
        protected XamlValueWithSideEffectNodeBase(IXamlLineInfo lineInfo, IXamlAstValueNode value) : base(lineInfo)
        {
            Value = value;
        }

        public IXamlAstValueNode Value { get; set; }
        public virtual IXamlAstTypeReference Type => Value.Type;

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
        }
    }
    
    public class XamlValueWithManipulationNode : XamlValueWithSideEffectNodeBase
    {
        public IXamlAstManipulationNode Manipulation { get; set; }

        public XamlValueWithManipulationNode(IXamlLineInfo lineInfo,
            IXamlAstValueNode value,
            IXamlAstManipulationNode manipulation) : base(lineInfo, value)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            base.VisitChildren(visitor);
            Manipulation = (IXamlAstManipulationNode) Manipulation?.Visit(visitor);
        }
    }

    public class XamlAstNewClrObjectNode : XamlAstNode, IXamlAstValueNode
    {
        public XamlAstNewClrObjectNode(IXamlLineInfo lineInfo,
            IXamlType type, IXamlConstructor ctor,
            List<IXamlAstValueNode> arguments) : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(lineInfo, type);
            Constructor = ctor;
            Arguments = arguments;
        }

        public IXamlAstTypeReference Type { get; set; }
        public IXamlConstructor Constructor { get; }
        public List<IXamlAstValueNode> Arguments { get; set; } = new List<IXamlAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
        }
    }

    public class XamlMarkupExtensionNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlAstValueNode Value { get; set; }
        public IXamlProperty Property { get; set; }
        public IXamlMethod ProvideValue { get; }
        public IXamlMethod Manipulation { get; set; }

        public XamlMarkupExtensionNode(IXamlLineInfo lineInfo, IXamlProperty property, IXamlMethod provideValue,
            IXamlAstValueNode value, IXamlMethod manipulation) : base(lineInfo)
        {
            Property = property;
            ProvideValue = provideValue;
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
        }
    }
    
    public class XamlObjectInitializationNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlAstManipulationNode Manipulation { get; set; }
        public IXamlType Type { get; set; }
        public XamlObjectInitializationNode(IXamlLineInfo lineInfo, 
            IXamlAstManipulationNode manipulation, IXamlType type) 
            : base(lineInfo)
        {
            Manipulation = manipulation;
            Type = type;
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Manipulation = (IXamlAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public class XamlToArrayNode : XamlAstNode, IXamlAstValueNode
    {
        public IXamlAstValueNode Value { get; set; }
        public XamlToArrayNode(IXamlLineInfo lineInfo, IXamlAstTypeReference arrayType,
            IXamlAstValueNode value) : base(lineInfo)
        {
            Type = arrayType;
            Value = value;
        }

        public IXamlAstTypeReference Type { get; }
    }

    public class XamlDeferredContentNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode
    {
        public IXamlAstValueNode Value { get; set; }
        public IXamlAstTypeReference Type { get; }
        
        public XamlDeferredContentNode(IXamlAstValueNode value, 
            XamlTransformerConfiguration config) : base(value)
        {
            Value = value;
            var funcType = config.TypeSystem.GetType("System.Func`2")
                .MakeGenericType(config.TypeMappings.ServiceProvider, config.WellKnownTypes.Object);
            Type = new XamlAstClrTypeReference(value, funcType);
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
        }

        void CompileBuilder(XamlEmitContext context)
        {
            var il = context.Emitter;
            // Initialize the context
            il
                .Ldarg_0()
                .Newobj(context.RuntimeContext.Constructor)
                .Stloc(context.ContextLocal);

            // It might be better to save this in a closure
            if (context.Configuration.TypeMappings.RootObjectProvider != null)
            {
                // Attempt to get the root object from parent service provider
                var noRoot = il.DefineLabel();
                using (var loc = context.GetLocal(context.Configuration.WellKnownTypes.Object))
                    il
                        // if(arg == null) goto noRoot;
                        .Ldarg_0()
                        .Brfalse(noRoot)
                        // var loc = arg.GetService(typeof(IRootObjectProvider))
                        .Ldarg_0()
                        .Ldtype(context.Configuration.TypeMappings.RootObjectProvider)
                        .EmitCall(context.Configuration.TypeMappings.ServiceProvider
                            .FindMethod(m => m.Name == "GetService"))
                        .Stloc(loc.Local)
                        // if(loc == null) goto noRoot;
                        .Ldloc(loc.Local)
                        .Brfalse(noRoot)
                        // loc = ((IRootObjectProvider)loc).RootObject
                        .Ldloc(loc.Local)
                        .Castclass(context.Configuration.TypeMappings.RootObjectProvider)
                        .EmitCall(context.Configuration.TypeMappings.RootObjectProvider
                            .FindMethod(m => m.Name == "get_RootObject"))
                        .Stloc(loc.Local)
                        // contextLocal.RootObject = loc;
                        .Ldloc(context.ContextLocal)
                        .Ldloc(loc.Local)
                        .Stfld(context.RuntimeContext.RootObjectField)
                        .MarkLabel(noRoot);
            }

            context.Emit(Value, context.Emitter, context.Configuration.WellKnownTypes.Object);
            il.Ret();
        }

        public XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen)
        {
            var so = context.Configuration.WellKnownTypes.Object;
            var isp = context.Configuration.TypeMappings.ServiceProvider;
            var subType = context.CreateSubType("XamlXClosure_" + Guid.NewGuid(), so);
            var buildMethod = subType.DefineMethod(so, new[]
            {
                isp
            }, "Build", true, true, false);
            CompileBuilder(new XamlEmitContext(buildMethod.Generator, context.Configuration,
                context.RuntimeContext, buildMethod.Generator.DefineLocal(context.RuntimeContext.ContextType),
                (s, type) => subType.DefineSubType(type, s, false), context.Emitters));

            var funcType = Type.GetClrType();
            codeGen
                .Ldnull()
                .Ldftn(buildMethod)
                .Newobj(funcType.Constructors.FirstOrDefault(ct =>
                    ct.Parameters.Count == 2 && ct.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));
            
            // Allow to save values from the parent context, pass own service provider, etc, etc
            if (context.Configuration.TypeMappings.DeferredContentExecutorCustomization != null)
            {
                codeGen
                    .Ldloc(context.ContextLocal)
                    .EmitCall(context.Configuration.TypeMappings.DeferredContentExecutorCustomization);
            }
            
            subType.CreateType();
            return XamlNodeEmitResult.Type(funcType);
        }
    }
}
