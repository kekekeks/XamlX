using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Transform;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlXAstVisitor;

namespace XamlX.Ast
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXAstClrTypeReference : XamlXAstNode, IXamlXAstTypeReference
    {
        public IXamlXType Type { get; }

        public XamlXAstClrTypeReference(IXamlXLineInfo lineInfo, IXamlXType type, bool isMarkupExtension) : base(lineInfo)
        {
            Type = type;
            IsMarkupExtension = isMarkupExtension;
        }

        public override string ToString() => Type.GetFqn();
        public bool IsMarkupExtension { get; }

        public bool Equals(IXamlXAstTypeReference other) =>
            other is XamlXAstClrTypeReference clr && clr.Type.Equals(Type) &&
            clr.IsMarkupExtension == IsMarkupExtension;
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXAstClrProperty : XamlXAstNode, IXamlXAstPropertyReference
    {
        public string Name { get; set; }
        public IXamlXMethod Getter { get; set; }
        public List<IXamlXPropertySetter> Setters { get; set; } = new List<IXamlXPropertySetter>();
        public List<IXamlXCustomAttribute> CustomAttributes { get; set; } = new List<IXamlXCustomAttribute>();
        public IXamlXType DeclaringType { get; set; }
        
        public XamlXAstClrProperty(IXamlXLineInfo lineInfo, IXamlXProperty property) : base(lineInfo)
        {
            Name = property.Name;
            Getter = property.Getter;
            if (property.Setter != null)
                Setters.Add(new XamlXDirectCallPropertySetter(property.Setter));
            CustomAttributes = property.CustomAttributes.ToList();
            DeclaringType = (property.Getter ?? property.Setter)?.DeclaringType;
        }

        public XamlXAstClrProperty(IXamlXLineInfo lineInfo, string name, IXamlXType declaringType, 
            IXamlXMethod getter, IEnumerable<IXamlXPropertySetter> setters) : base(lineInfo)
        {
            Name = name;
            DeclaringType = declaringType;
            Getter = getter;
            if (setters != null)
                Setters.AddRange(setters);
        }

        public XamlXAstClrProperty(IXamlXLineInfo lineInfo, string name, IXamlXType declaringType,
            IXamlXMethod getter, params IXamlXMethod[] setters) : this(lineInfo, name, declaringType,
            getter, setters.Select(x => new XamlXDirectCallPropertySetter(x)))
        {

        }

        public override string ToString() => DeclaringType.GetFqn() + "." + Name;
    }

    class XamlXDirectCallPropertySetter : IXamlXPropertySetter
    {
        private readonly IXamlXMethod _method;
        public IXamlXType TargetType { get; }
        public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();
        public IReadOnlyList<IXamlXType> Parameters { get; }
        public void Emit(IXamlXEmitter codegen)
        {
            codegen.EmitCall(_method, true);
        }

        public XamlXDirectCallPropertySetter(IXamlXMethod method)
        {
            _method = method;
            Parameters = method.ParametersWithThis().Skip(1).ToList();
            TargetType = method.ThisOrFirstParameter();
        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class PropertySetterBinderParameters
    {
        public bool AllowMultiple { get; set; }
        public bool AllowXNull { get; set; } = true;
        public bool AllowRuntimeNull { get; set; } = true;
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXPropertySetter
    {
        IXamlXType TargetType { get; }
        PropertySetterBinderParameters BinderParameters { get; }
        IReadOnlyList<IXamlXType> Parameters { get; }
        void Emit(IXamlXEmitter codegen);
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXPropertyAssignmentNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public XamlXAstClrProperty Property { get; }
        public List<IXamlXPropertySetter> PossibleSetters { get; set; }
        public List<IXamlXAstValueNode> Values { get; set; }

        public XamlXPropertyAssignmentNode(IXamlXLineInfo lineInfo,
            XamlXAstClrProperty property,
            IEnumerable<IXamlXPropertySetter> setters, IEnumerable<IXamlXAstValueNode> values)
            : base(lineInfo)
        {
            Property = property;
            PossibleSetters = setters.ToList();
            Values = values.ToList();
        }

        public override void VisitChildren(Visitor visitor)
        {
            VisitList(Values, visitor);
        }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXPropertyValueManipulationNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public XamlXAstClrProperty Property { get; set; }
        public IXamlXAstManipulationNode Manipulation { get; set; }
        public XamlXPropertyValueManipulationNode(IXamlXLineInfo lineInfo, 
            XamlXAstClrProperty property, IXamlXAstManipulationNode manipulation) 
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

#if !XAMLIL_INTERNAL
    public
#endif
    abstract class XamlXMethodCallBaseNode : XamlXAstNode
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
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXNoReturnMethodCallNode : XamlXMethodCallBaseNode, IXamlXAstManipulationNode
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

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXStaticOrTargetedReturnMethodCallNode : XamlXMethodCallBaseNode, IXamlXAstValueNode
    {
        public XamlXStaticOrTargetedReturnMethodCallNode(IXamlXLineInfo lineInfo, IXamlXWrappedMethod method,
            IEnumerable<IXamlXAstValueNode> args)
            : base(lineInfo, method, args)
        {
            Type = new XamlXAstClrTypeReference(lineInfo, method.ReturnType, false);
        }

        public XamlXStaticOrTargetedReturnMethodCallNode(IXamlXLineInfo lineInfo, IXamlXMethod method,
            IEnumerable<IXamlXAstValueNode> args)
            : this(lineInfo, new XamlXWrappedMethod(method), args)
        {
            
        }

        public IXamlXAstTypeReference Type { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXManipulationGroupNode : XamlXAstNode, IXamlXAstManipulationNode
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

#if !XAMLIL_INTERNAL
    public
#endif
    abstract class XamlXValueWithSideEffectNodeBase : XamlXAstNode, IXamlXAstValueNode
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
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXValueWithManipulationNode : XamlXValueWithSideEffectNodeBase
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

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXAstNewClrObjectNode : XamlXAstNode, IXamlXAstValueNode
    {
        public XamlXAstNewClrObjectNode(IXamlXLineInfo lineInfo,
            XamlXAstClrTypeReference type, IXamlXConstructor ctor,
            List<IXamlXAstValueNode> arguments) : base(lineInfo)
        {
            Type = type;
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

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXMarkupExtensionNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstNodeNeedsParentStack
    {
        public IXamlXAstValueNode Value { get; set; }
        public IXamlXMethod ProvideValue { get; }

        public XamlXMarkupExtensionNode(IXamlXLineInfo lineInfo, IXamlXMethod provideValue,
            IXamlXAstValueNode value) : base(lineInfo)
        {
            ProvideValue = provideValue;
            Value = value;
            Type = new XamlXAstClrTypeReference(this, ProvideValue.ReturnType, false);
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
        }

        public bool NeedsParentStack => ProvideValue?.Parameters.Count > 0;
        public IXamlXAstTypeReference Type { get; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXObjectInitializationNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXAstManipulationNode Manipulation { get; set; }
        public IXamlXType Type { get; set; }
        public bool SkipBeginInit { get; set; }
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

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXToArrayNode : XamlXAstNode, IXamlXAstValueNode
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
    
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXWrappedMethod
    {
        string Name { get; }
        IXamlXType ReturnType { get; }
        IXamlXType DeclaringType { get; }
        IReadOnlyList<IXamlXType> ParametersWithThis { get; }
        void Emit(XamlXEmitContext context, IXamlXEmitter codeGen, bool swallowResult);
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXWrappedMethod : IXamlXWrappedMethod
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
        public IXamlXType DeclaringType => _method.DeclaringType;
        public IReadOnlyList<IXamlXType> ParametersWithThis { get; }
        public void Emit(XamlXEmitContext context, IXamlXEmitter codeGen, bool swallowResult)
        {
            codeGen.EmitCall(_method, swallowResult);
        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXWrappedMethodWithCasts : IXamlXWrappedMethod
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
        public IXamlXType DeclaringType => _method.DeclaringType;
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
                var locals = new Stack<XamlXLocalsPool.PooledLocal>();
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
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXMethodWithCasts : IXamlXCustomEmitMethod
    {
        private readonly IXamlXMethod _method;
        private readonly IReadOnlyList<IXamlXType> _baseParametersWithThis;

        public XamlXMethodWithCasts(IXamlXMethod method, IEnumerable<IXamlXType> newArgumentTypes)
        {
            _method = method;
            Parameters = newArgumentTypes.ToList();
            _baseParametersWithThis = _method.ParametersWithThis();
            if (_baseParametersWithThis.Count != Parameters.Count)
                throw new ArgumentException("Method argument count mismatch");
        }

        public string Name => _method.Name;
        public IXamlXType ReturnType => _method.ReturnType;
        public IXamlXType DeclaringType => _method.DeclaringType;
        public bool IsPublic => true;
        public bool IsStatic => true;
        public IReadOnlyList<IXamlXType> Parameters { get; }
        public void EmitCall(IXamlXEmitter codeGen)
        {
            int firstCast = -1; 
            for (var c = Parameters.Count - 1; c >= 0; c--)
            {
                if (!_baseParametersWithThis[c].Equals(Parameters[c]))
                    firstCast = c;
            }

            if (firstCast != -1)
            {
                var locals = new Stack<XamlXLocalsPool.PooledLocal>();
                for (var c = Parameters.Count - 1; c >= firstCast; c--)
                {
                    codeGen.Castclass(_baseParametersWithThis[c]);
                    if (c > firstCast)
                    {
                        var l = codeGen.LocalsPool.GetLocal(_baseParametersWithThis[c]);
                        codeGen.Stloc(l.Local);
                        locals.Push(l);
                    }
                }

                while (locals.Count!=0)
                {
                    using (var l = locals.Pop())
                    {
                        codeGen.Ldloc(l.Local);
                        l.Dispose();
                    }
                }
            }

            codeGen.EmitCall(_method);
        }

        public bool Equals(IXamlXMethod other) =>
            other is XamlXMethodWithCasts mwc && mwc._method.Equals(_method) &&
            mwc.Parameters.SequenceEqual(Parameters);

        public IXamlXMethod MakeGenericMethod(IReadOnlyList<IXamlXType> typeArguments)
        {
            throw new InvalidOperationException();
        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXDeferredContentNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public IXamlXAstValueNode Value { get; set; }
        public IXamlXAstTypeReference Type { get; }
        
        public XamlXDeferredContentNode(IXamlXAstValueNode value, 
            XamlXTransformerConfiguration config) : base(value)
        {
            Value = value;
            var funcType = config.TypeSystem.GetType("System.Func`2")
                .MakeGenericType(config.TypeMappings.ServiceProvider, config.WellKnownTypes.Object);
            Type = new XamlXAstClrTypeReference(value, funcType, false);
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
            return XamlXNodeEmitResult.Type(0, funcType);
        }
    }
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXDeferredContentInitializeIntermediateRootNode 
        : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public IXamlXAstValueNode Value { get; set; }

        public XamlXDeferredContentInitializeIntermediateRootNode(IXamlXAstValueNode value) : base(value)
        {
            Value = value;
        }
        
        public override void VisitChildren(IXamlXAstVisitor visitor)
        {
            Value = (IXamlXAstValueNode) Value.Visit(visitor);
        }

        public IXamlXAstTypeReference Type => Value.Type;
        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            codeGen
                .Ldloc(context.ContextLocal);
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            codeGen
                .Stfld(context.RuntimeContext.IntermediateRootObjectField)
                .Ldloc(context.ContextLocal)
                .Ldfld(context.RuntimeContext.IntermediateRootObjectField);
            return XamlXNodeEmitResult.Type(0, Value.Type.GetClrType());
        }


    }
}
