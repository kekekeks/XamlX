using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Transform;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlXAstVisitor;

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

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
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

        public override void VisitChildren(Visitor visitor)
        {
            Manipulation = (IXamlXAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public abstract class XamlXMethodCallBaseNode : XamlXAstNode
    {
        public IXamlXWrappedMethod Method { get; set; }
        public List<IXamlXAstValueNode> Arguments { get; set; }
        public XamlXMethodCallBaseNode(IXamlXLineInfo lineInfo, 
            IXamlXWrappedMethod method, IEnumerable<IXamlXAstValueNode> args) 
            : base(lineInfo)
        {
            Method = method;
            Arguments = args?.ToList() ?? new List<IXamlXAstValueNode>();
        }

        public override void VisitChildren(Visitor visitor)
        {
            VisitList(Arguments, visitor);
        }
    }
    
    public class XamlXNoReturnMethodCallNode : XamlXMethodCallBaseNode, IXamlXAstManipulationNode
    {
        public XamlXNoReturnMethodCallNode(IXamlXLineInfo lineInfo, IXamlXMethod method, IEnumerable<IXamlXAstValueNode> args)
            : base(lineInfo, new XamlXWrappedMethod(method), args)
        {
        }
        
        public XamlXNoReturnMethodCallNode(IXamlXLineInfo lineInfo, IXamlXWrappedMethod method, IEnumerable<IXamlXAstValueNode> args)
            : base(lineInfo, method, args)
        {
        }
    }

    public class XamlXStaticOrTargetedReturnMethodCallNode : XamlXMethodCallBaseNode, IXamlXAstValueNode
    {
        public XamlXStaticOrTargetedReturnMethodCallNode(IXamlXLineInfo lineInfo, IXamlXWrappedMethod method,
            IEnumerable<IXamlXAstValueNode> args)
            : base(lineInfo, method, args)
        {
            Type = new XamlXAstClrTypeReference(lineInfo, method.ReturnType);
        }

        public XamlXStaticOrTargetedReturnMethodCallNode(IXamlXLineInfo lineInfo, IXamlXMethod method,
            IEnumerable<IXamlXAstValueNode> args)
            : this(lineInfo, new XamlXWrappedMethod(method), args)
        {
            
        }

        public IXamlXAstTypeReference Type { get; }
    }

    public class XamlXManipulationGroupNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public List<IXamlXAstManipulationNode> Children { get; set; } = new List<IXamlXAstManipulationNode>();

        public XamlXManipulationGroupNode(IXamlXLineInfo lineInfo,
            IEnumerable<IXamlXAstManipulationNode> children = null)
            : base(lineInfo)
        {
            if (children != null)
                Children.AddRange(children);
        }

        public override void VisitChildren(Visitor visitor) => VisitList(Children, visitor);
    }

    public abstract class XamlXValueWithSideEffectNodeBase : XamlXAstNode, IXamlXAstValueNode
    {
        protected XamlXValueWithSideEffectNodeBase(IXamlXLineInfo lineInfo, IXamlXAstValueNode value) : base(lineInfo)
        {
            Value = value;
        }

        public IXamlXAstValueNode Value { get; set; }
        public virtual IXamlXAstTypeReference Type => Value.Type;

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
        }
    }
    
    public class XamlXValueWithManipulationNode : XamlXValueWithSideEffectNodeBase
    {
        public IXamlXAstManipulationNode Manipulation { get; set; }

        public XamlXValueWithManipulationNode(IXamlXLineInfo lineInfo,
            IXamlXAstValueNode value,
            IXamlXAstManipulationNode manipulation) : base(lineInfo, value)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(Visitor visitor)
        {
            base.VisitChildren(visitor);
            Manipulation = (IXamlXAstManipulationNode) Manipulation?.Visit(visitor);
        }
    }

    public class XamlXAstNewClrObjectNode : XamlXAstNode, IXamlXAstValueNode
    {
        public XamlXAstNewClrObjectNode(IXamlXLineInfo lineInfo,
            IXamlXType type, IXamlXConstructor ctor,
            List<IXamlXAstValueNode> arguments) : base(lineInfo)
        {
            Type = new XamlXAstClrTypeReference(lineInfo, type);
            Constructor = ctor;
            Arguments = arguments;
        }

        public IXamlXAstTypeReference Type { get; set; }
        public IXamlXConstructor Constructor { get; }
        public List<IXamlXAstValueNode> Arguments { get; set; } = new List<IXamlXAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlXAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
        }
    }

    public class XamlXMarkupExtensionNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXAstValueNode Value { get; set; }
        public IXamlXProperty Property { get; set; }
        public IXamlXMethod ProvideValue { get; }
        public IXamlXWrappedMethod Manipulation { get; set; }

        public XamlXMarkupExtensionNode(IXamlXLineInfo lineInfo, IXamlXProperty property, IXamlXMethod provideValue,
            IXamlXAstValueNode value, IXamlXWrappedMethod manipulation) : base(lineInfo)
        {
            Property = property;
            ProvideValue = provideValue;
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
        }
    }
    
    public class XamlXObjectInitializationNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXAstManipulationNode Manipulation { get; set; }
        public IXamlXType Type { get; set; }
        public XamlXObjectInitializationNode(IXamlXLineInfo lineInfo, 
            IXamlXAstManipulationNode manipulation, IXamlXType type) 
            : base(lineInfo)
        {
            Manipulation = manipulation;
            Type = type;
        }

        public override void VisitChildren(Visitor visitor)
        {
            Manipulation = (IXamlXAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public class XamlXToArrayNode : XamlXAstNode, IXamlXAstValueNode
    {
        public IXamlXAstValueNode Value { get; set; }
        public XamlXToArrayNode(IXamlXLineInfo lineInfo, IXamlXAstTypeReference arrayType,
            IXamlXAstValueNode value) : base(lineInfo)
        {
            Type = arrayType;
            Value = value;
        }

        public IXamlXAstTypeReference Type { get; }
    }
    
    
    public interface IXamlXWrappedMethod
    {
        string Name { get; }
        IXamlXType ReturnType { get; }
        IReadOnlyList<IXamlXType> ParametersWithThis { get; }
        void Emit(XamlXEmitContext context, IXamlXEmitter codeGen, bool swallowResult);
    }

    public class XamlXWrappedMethod : IXamlXWrappedMethod
    {
        private readonly IXamlXMethod _method;

        public XamlXWrappedMethod(IXamlXMethod method)
        {
            _method = method;
            ParametersWithThis =
                method.IsStatic ? method.Parameters : new[] {method.DeclaringType}.Concat(method.Parameters).ToList();
            ReturnType = method.ReturnType;
        }

        public string Name => _method.Name;
        public IXamlXType ReturnType { get; }
        public IReadOnlyList<IXamlXType> ParametersWithThis { get; }
        public void Emit(XamlXEmitContext context, IXamlXEmitter codeGen, bool swallowResult)
        {
            codeGen.EmitCall(_method, swallowResult);
        }
    }

    public class XamlXWrappedMethodWithCasts : IXamlXWrappedMethod
    {
        private readonly IXamlXWrappedMethod _method;

        public XamlXWrappedMethodWithCasts(IXamlXWrappedMethod method, IEnumerable<IXamlXType> newArgumentTypes)
        {
            _method = method;
            ParametersWithThis = newArgumentTypes.ToList();
            if (_method.ParametersWithThis.Count != ParametersWithThis.Count)
                throw new ArgumentException("Method argument count mismatch");
        }

        public string Name => _method.Name;
        public IXamlXType ReturnType => _method.ReturnType;
        public IReadOnlyList<IXamlXType> ParametersWithThis { get; }
        public void Emit(XamlXEmitContext context, IXamlXEmitter codeGen, bool swallowResult)
        {
            int firstCast = -1; 
            for (var c = ParametersWithThis.Count - 1; c >= 0; c--)
            {
                if (!_method.ParametersWithThis[c].Equals(ParametersWithThis[c]))
                    firstCast = c;
            }

            if (firstCast != -1)
            {
                var locals = new Stack<XamlXEmitContext.PooledLocal>();
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
    

    public class XamlXDeferredContentNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public IXamlXAstValueNode Value { get; set; }
        public IXamlXAstTypeReference Type { get; }
        
        public XamlXDeferredContentNode(IXamlXAstValueNode value, 
            XamlXTransformerConfiguration config) : base(value)
        {
            Value = value;
            var funcType = config.TypeSystem.GetType("System.Func`2")
                .MakeGenericType(config.TypeMappings.ServiceProvider, config.WellKnownTypes.Object);
            Type = new XamlXAstClrTypeReference(value, funcType);
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
        }

        void CompileBuilder(XamlXEmitContext context)
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

        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            var so = context.Configuration.WellKnownTypes.Object;
            var isp = context.Configuration.TypeMappings.ServiceProvider;
            var subType = context.CreateSubType("XamlXClosure_" + Guid.NewGuid(), so);
            var buildMethod = subType.DefineMethod(so, new[]
            {
                isp
            }, "Build", true, true, false);
            CompileBuilder(new XamlXEmitContext(buildMethod.Generator, context.Configuration,
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
            return XamlXNodeEmitResult.Type(0, funcType);
        }
    }
}
