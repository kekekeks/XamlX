using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Transform;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlAstVisitor;

namespace XamlX.Ast
{
    public class XamlAstClrTypeReference : XamlAstNode, IXamlAstTypeReference
    {
        public IXamlType Type { get; }

        public XamlAstClrTypeReference(IXamlLineInfo lineInfo, IXamlType type, bool isMarkupExtension) : base(lineInfo)
        {
            Type = type;
            IsMarkupExtension = isMarkupExtension;
        }

        public override string ToString() => Type.GetFqn();
        public bool IsMarkupExtension { get; }
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

        public override void VisitChildren(Visitor visitor)
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

        public override void VisitChildren(Visitor visitor)
        {
            Manipulation = (IXamlAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public abstract class XamlMethodCallBaseNode : XamlAstNode
    {
        public IXamlWrappedMethod Method { get; set; }
        public List<IXamlAstValueNode> Arguments { get; set; }
        public XamlMethodCallBaseNode(IXamlLineInfo lineInfo, 
            IXamlWrappedMethod method, IEnumerable<IXamlAstValueNode> args) 
            : base(lineInfo)
        {
            Method = method;
            Arguments = args?.ToList() ?? new List<IXamlAstValueNode>();
        }

        public override void VisitChildren(Visitor visitor)
        {
            VisitList(Arguments, visitor);
        }
    }
    
    public class XamlNoReturnMethodCallNode : XamlMethodCallBaseNode, IXamlAstManipulationNode
    {
        public XamlNoReturnMethodCallNode(IXamlLineInfo lineInfo, IXamlMethod method, IEnumerable<IXamlAstValueNode> args)
            : base(lineInfo, new XamlWrappedMethod(method), args)
        {
        }
        
        public XamlNoReturnMethodCallNode(IXamlLineInfo lineInfo, IXamlWrappedMethod method, IEnumerable<IXamlAstValueNode> args)
            : base(lineInfo, method, args)
        {
        }
    }

    public class XamlStaticOrTargetedReturnMethodCallNode : XamlMethodCallBaseNode, IXamlAstValueNode
    {
        public XamlStaticOrTargetedReturnMethodCallNode(IXamlLineInfo lineInfo, IXamlWrappedMethod method,
            IEnumerable<IXamlAstValueNode> args)
            : base(lineInfo, method, args)
        {
            Type = new XamlAstClrTypeReference(lineInfo, method.ReturnType, false);
        }

        public XamlStaticOrTargetedReturnMethodCallNode(IXamlLineInfo lineInfo, IXamlMethod method,
            IEnumerable<IXamlAstValueNode> args)
            : this(lineInfo, new XamlWrappedMethod(method), args)
        {
            
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

        public override void VisitChildren(Visitor visitor) => VisitList(Children, visitor);
    }

    public abstract class XamlValueWithSideEffectNodeBase : XamlAstNode, IXamlAstValueNode
    {
        protected XamlValueWithSideEffectNodeBase(IXamlLineInfo lineInfo, IXamlAstValueNode value) : base(lineInfo)
        {
            Value = value;
        }

        public IXamlAstValueNode Value { get; set; }
        public virtual IXamlAstTypeReference Type => Value.Type;

        public override void VisitChildren(Visitor visitor)
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

        public override void VisitChildren(Visitor visitor)
        {
            base.VisitChildren(visitor);
            Manipulation = (IXamlAstManipulationNode) Manipulation?.Visit(visitor);
        }
    }

    public class XamlAstNewClrObjectNode : XamlAstNode, IXamlAstValueNode
    {
        public XamlAstNewClrObjectNode(IXamlLineInfo lineInfo,
            XamlAstClrTypeReference type, IXamlConstructor ctor,
            List<IXamlAstValueNode> arguments) : base(lineInfo)
        {
            Type = type;
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

    public class XamlMarkupExtensionNode : XamlAstNode, IXamlAstManipulationNode, IXamlAstNodeNeedsParentStack
    {
        public IXamlAstValueNode Value { get; set; }
        public IXamlProperty Property { get; set; }
        public IXamlMethod ProvideValue { get; }
        public IXamlWrappedMethod Manipulation { get; set; }

        public XamlMarkupExtensionNode(IXamlLineInfo lineInfo, IXamlProperty property, IXamlMethod provideValue,
            IXamlAstValueNode value, IXamlWrappedMethod manipulation) : base(lineInfo)
        {
            Property = property;
            ProvideValue = provideValue;
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
        }

        public bool NeedsParentStack => ProvideValue?.Parameters.Count > 0;
    }
    
    public class XamlObjectInitializationNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlAstManipulationNode Manipulation { get; set; }
        public IXamlType Type { get; set; }
        public bool SkipBeginInit { get; set; }
        public XamlObjectInitializationNode(IXamlLineInfo lineInfo, 
            IXamlAstManipulationNode manipulation, IXamlType type) 
            : base(lineInfo)
        {
            Manipulation = manipulation;
            Type = type;
        }

        public override void VisitChildren(Visitor visitor)
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
    
    
    public interface IXamlWrappedMethod
    {
        string Name { get; }
        IXamlType ReturnType { get; }
        IXamlType DeclaringType { get; }
        IReadOnlyList<IXamlType> ParametersWithThis { get; }
        void Emit(XamlEmitContext context, IXamlILEmitter codeGen, bool swallowResult);
    }

    public class XamlWrappedMethod : IXamlWrappedMethod
    {
        private readonly IXamlMethod _method;

        public XamlWrappedMethod(IXamlMethod method)
        {
            _method = method;
            ParametersWithThis =
                method.IsStatic ? method.Parameters : new[] {method.DeclaringType}.Concat(method.Parameters).ToList();
            ReturnType = method.ReturnType;
        }

        public string Name => _method.Name;
        public IXamlType ReturnType { get; }
        public IXamlType DeclaringType => _method.DeclaringType;
        public IReadOnlyList<IXamlType> ParametersWithThis { get; }
        public void Emit(XamlEmitContext context, IXamlILEmitter codeGen, bool swallowResult)
        {
            codeGen.EmitCall(_method, swallowResult);
        }
    }

    public class XamlWrappedMethodWithCasts : IXamlWrappedMethod
    {
        private readonly IXamlWrappedMethod _method;

        public XamlWrappedMethodWithCasts(IXamlWrappedMethod method, IEnumerable<IXamlType> newArgumentTypes)
        {
            _method = method;
            ParametersWithThis = newArgumentTypes.ToList();
            if (_method.ParametersWithThis.Count != ParametersWithThis.Count)
                throw new ArgumentException("Method argument count mismatch");
        }

        public string Name => _method.Name;
        public IXamlType ReturnType => _method.ReturnType;
        public IXamlType DeclaringType => _method.DeclaringType;
        public IReadOnlyList<IXamlType> ParametersWithThis { get; }
        public void Emit(XamlEmitContext context, IXamlILEmitter codeGen, bool swallowResult)
        {
            int firstCast = -1; 
            for (var c = ParametersWithThis.Count - 1; c >= 0; c--)
            {
                if (!_method.ParametersWithThis[c].Equals(ParametersWithThis[c]))
                    firstCast = c;
            }

            if (firstCast != -1)
            {
                var locals = new Stack<XamlEmitContext.PooledLocal>();
                for (var c = ParametersWithThis.Count - 1; c >= firstCast; c--)
                {
                    codeGen.Castclass(_method.ParametersWithThis[c]);
                    if (c > firstCast)
                    {
                        var l = context.GetLocal(_method.ParametersWithThis[c]);
                        codeGen.Stloc(l.Local);
                        locals.Push(l);
                    }
                }

                while (locals.Count!=0)
                {
                    using (var l = locals.Pop())
                        codeGen.Ldloc(l.Local);
                }
            }

            _method.Emit(context, codeGen, swallowResult);
        }
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
            Type = new XamlAstClrTypeReference(value, funcType, false);
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
        }

        void CompileBuilder(XamlEmitContext context)
        {
            var il = context.Emitter;
            // Initialize the context
            il
                .Ldarg_0();
            context.RuntimeContext.Factory(il);    
            il.Stloc(context.ContextLocal);

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
                        .Castclass(context.RuntimeContext.ContextType.GenericArguments[0])
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
                (s, type) => subType.DefineSubType(type, s, false), context.File, context.Emitters));

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
            return XamlNodeEmitResult.Type(0, funcType);
        }
    }
}
