using System;
using System.Collections.Generic;
using System.Linq;
using XamlIl.Transform;
using XamlIl.TypeSystem;
using Visitor = XamlIl.Ast.IXamlIlAstVisitor;

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

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlIlAstValueNode) Value.Visit(visitor);
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

        public override void VisitChildren(Visitor visitor)
        {
            Manipulation = (IXamlIlAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public abstract class XamlIlMethodCallBaseNode : XamlIlAstNode
    {
        public IXamlIlWrappedMethod Method { get; set; }
        public List<IXamlIlAstValueNode> Arguments { get; set; }
        public XamlIlMethodCallBaseNode(IXamlIlLineInfo lineInfo, 
            IXamlIlWrappedMethod method, IEnumerable<IXamlIlAstValueNode> args) 
            : base(lineInfo)
        {
            Method = method;
            Arguments = args?.ToList() ?? new List<IXamlIlAstValueNode>();
        }

        public override void VisitChildren(Visitor visitor)
        {
            VisitList(Arguments, visitor);
        }
    }
    
    public class XamlIlNoReturnMethodCallNode : XamlIlMethodCallBaseNode, IXamlIlAstManipulationNode
    {
        public XamlIlNoReturnMethodCallNode(IXamlIlLineInfo lineInfo, IXamlIlMethod method, IEnumerable<IXamlIlAstValueNode> args)
            : base(lineInfo, new XamlIlWrappedMethod(method), args)
        {
        }
        
        public XamlIlNoReturnMethodCallNode(IXamlIlLineInfo lineInfo, IXamlIlWrappedMethod method, IEnumerable<IXamlIlAstValueNode> args)
            : base(lineInfo, method, args)
        {
        }
    }

    public class XamlIlStaticOrTargetedReturnMethodCallNode : XamlIlMethodCallBaseNode, IXamlIlAstValueNode
    {
        public XamlIlStaticOrTargetedReturnMethodCallNode(IXamlIlLineInfo lineInfo, IXamlIlWrappedMethod method,
            IEnumerable<IXamlIlAstValueNode> args)
            : base(lineInfo, method, args)
        {
            Type = new XamlIlAstClrTypeReference(lineInfo, method.ReturnType);
        }

        public XamlIlStaticOrTargetedReturnMethodCallNode(IXamlIlLineInfo lineInfo, IXamlIlMethod method,
            IEnumerable<IXamlIlAstValueNode> args)
            : this(lineInfo, new XamlIlWrappedMethod(method), args)
        {
            
        }

        public IXamlIlAstTypeReference Type { get; }
    }

    public class XamlIlManipulationGroupNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public List<IXamlIlAstManipulationNode> Children { get; set; } = new List<IXamlIlAstManipulationNode>();

        public XamlIlManipulationGroupNode(IXamlIlLineInfo lineInfo,
            IEnumerable<IXamlIlAstManipulationNode> children = null)
            : base(lineInfo)
        {
            if (children != null)
                Children.AddRange(children);
        }

        public override void VisitChildren(Visitor visitor) => VisitList(Children, visitor);
    }

    public abstract class XamlIlValueWithSideEffectNodeBase : XamlIlAstNode, IXamlIlAstValueNode
    {
        protected XamlIlValueWithSideEffectNodeBase(IXamlIlLineInfo lineInfo, IXamlIlAstValueNode value) : base(lineInfo)
        {
            Value = value;
        }

        public IXamlIlAstValueNode Value { get; set; }
        public virtual IXamlIlAstTypeReference Type => Value.Type;

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlIlAstValueNode) Value.Visit(visitor);
        }
    }
    
    public class XamlIlValueWithManipulationNode : XamlIlValueWithSideEffectNodeBase
    {
        public IXamlIlAstManipulationNode Manipulation { get; set; }

        public XamlIlValueWithManipulationNode(IXamlIlLineInfo lineInfo,
            IXamlIlAstValueNode value,
            IXamlIlAstManipulationNode manipulation) : base(lineInfo, value)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(Visitor visitor)
        {
            base.VisitChildren(visitor);
            Manipulation = (IXamlIlAstManipulationNode) Manipulation?.Visit(visitor);
        }
    }

    public class XamlIlAstNewClrObjectNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public XamlIlAstNewClrObjectNode(IXamlIlLineInfo lineInfo,
            IXamlIlType type, IXamlIlConstructor ctor,
            List<IXamlIlAstValueNode> arguments) : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(lineInfo, type);
            Constructor = ctor;
            Arguments = arguments;
        }

        public IXamlIlAstTypeReference Type { get; set; }
        public IXamlIlConstructor Constructor { get; }
        public List<IXamlIlAstValueNode> Arguments { get; set; } = new List<IXamlIlAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlIlAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
        }
    }

    public class XamlIlMarkupExtensionNode : XamlIlAstNode, IXamlIlAstManipulationNode, IXamlIlAstNodeNeedsParentStack
    {
        public IXamlIlAstValueNode Value { get; set; }
        public IXamlIlProperty Property { get; set; }
        public IXamlIlMethod ProvideValue { get; }
        public IXamlIlWrappedMethod Manipulation { get; set; }

        public XamlIlMarkupExtensionNode(IXamlIlLineInfo lineInfo, IXamlIlProperty property, IXamlIlMethod provideValue,
            IXamlIlAstValueNode value, IXamlIlWrappedMethod manipulation) : base(lineInfo)
        {
            Property = property;
            ProvideValue = provideValue;
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlIlAstValueNode) Value.Visit(visitor);
        }

        public bool NeedsParentStack => ProvideValue?.Parameters.Count > 0;
    }
    
    public class XamlIlObjectInitializationNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public IXamlIlAstManipulationNode Manipulation { get; set; }
        public IXamlIlType Type { get; set; }
        public bool SkipBeginInit { get; set; }
        public XamlIlObjectInitializationNode(IXamlIlLineInfo lineInfo, 
            IXamlIlAstManipulationNode manipulation, IXamlIlType type) 
            : base(lineInfo)
        {
            Manipulation = manipulation;
            Type = type;
        }

        public override void VisitChildren(Visitor visitor)
        {
            Manipulation = (IXamlIlAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public class XamlIlToArrayNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public IXamlIlAstValueNode Value { get; set; }
        public XamlIlToArrayNode(IXamlIlLineInfo lineInfo, IXamlIlAstTypeReference arrayType,
            IXamlIlAstValueNode value) : base(lineInfo)
        {
            Type = arrayType;
            Value = value;
        }

        public IXamlIlAstTypeReference Type { get; }
    }
    
    
    public interface IXamlIlWrappedMethod
    {
        string Name { get; }
        IXamlIlType ReturnType { get; }
        IReadOnlyList<IXamlIlType> ParametersWithThis { get; }
        void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen, bool swallowResult);
    }

    public class XamlIlWrappedMethod : IXamlIlWrappedMethod
    {
        private readonly IXamlIlMethod _method;

        public XamlIlWrappedMethod(IXamlIlMethod method)
        {
            _method = method;
            ParametersWithThis =
                method.IsStatic ? method.Parameters : new[] {method.DeclaringType}.Concat(method.Parameters).ToList();
            ReturnType = method.ReturnType;
        }

        public string Name => _method.Name;
        public IXamlIlType ReturnType { get; }
        public IReadOnlyList<IXamlIlType> ParametersWithThis { get; }
        public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen, bool swallowResult)
        {
            codeGen.EmitCall(_method, swallowResult);
        }
    }

    public class XamlIlWrappedMethodWithCasts : IXamlIlWrappedMethod
    {
        private readonly IXamlIlWrappedMethod _method;

        public XamlIlWrappedMethodWithCasts(IXamlIlWrappedMethod method, IEnumerable<IXamlIlType> newArgumentTypes)
        {
            _method = method;
            ParametersWithThis = newArgumentTypes.ToList();
            if (_method.ParametersWithThis.Count != ParametersWithThis.Count)
                throw new ArgumentException("Method argument count mismatch");
        }

        public string Name => _method.Name;
        public IXamlIlType ReturnType => _method.ReturnType;
        public IReadOnlyList<IXamlIlType> ParametersWithThis { get; }
        public void Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen, bool swallowResult)
        {
            int firstCast = -1; 
            for (var c = ParametersWithThis.Count - 1; c >= 0; c--)
            {
                if (!_method.ParametersWithThis[c].Equals(ParametersWithThis[c]))
                    firstCast = c;
            }

            if (firstCast != -1)
            {
                var locals = new Stack<XamlIlEmitContext.PooledLocal>();
                for (var c = ParametersWithThis.Count - 1; c >= firstCast; c--)
                {
                    codeGen.Castclass(ParametersWithThis[c]);
                    if (c > firstCast)
                    {
                        var l = context.GetLocal(ParametersWithThis[c]);
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
    

    public class XamlIlDeferredContentNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public IXamlIlAstValueNode Value { get; set; }
        public IXamlIlAstTypeReference Type { get; }
        
        public XamlIlDeferredContentNode(IXamlIlAstValueNode value, 
            XamlIlTransformerConfiguration config) : base(value)
        {
            Value = value;
            var funcType = config.TypeSystem.GetType("System.Func`2")
                .MakeGenericType(config.TypeMappings.ServiceProvider, config.WellKnownTypes.Object);
            Type = new XamlIlAstClrTypeReference(value, funcType);
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlIlAstValueNode) Value.Visit(visitor);
        }

        void CompileBuilder(XamlIlEmitContext context)
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
                        .Stfld(context.RuntimeContext.RootObjectField)
                        .MarkLabel(noRoot);
            }

            context.Emit(Value, context.Emitter, context.Configuration.WellKnownTypes.Object);
            il.Ret();
        }

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            var so = context.Configuration.WellKnownTypes.Object;
            var isp = context.Configuration.TypeMappings.ServiceProvider;
            var subType = context.CreateSubType("XamlIlClosure_" + Guid.NewGuid(), so);
            var buildMethod = subType.DefineMethod(so, new[]
            {
                isp
            }, "Build", true, true, false);
            CompileBuilder(new XamlIlEmitContext(buildMethod.Generator, context.Configuration,
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
            return XamlIlNodeEmitResult.Type(0, funcType);
        }
    }
}
