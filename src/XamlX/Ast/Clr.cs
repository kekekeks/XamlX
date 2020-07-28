using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlAstVisitor;

namespace XamlX.Ast
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstClrTypeReference : XamlAstNode, IXamlAstTypeReference
    {
        public IXamlType Type { get; }

        public XamlAstClrTypeReference(IXamlLineInfo lineInfo, IXamlType type, bool isMarkupExtension) : base(lineInfo)
        {
            Type = type;
            IsMarkupExtension = isMarkupExtension;
        }

        public override string ToString() => Type.GetFqn();
        public bool IsMarkupExtension { get; }

        public bool Equals(IXamlAstTypeReference other) =>
            other is XamlAstClrTypeReference clr && clr.Type.Equals(Type) &&
            clr.IsMarkupExtension == IsMarkupExtension;
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstClrProperty : XamlAstNode, IXamlAstPropertyReference
    {
        public string Name { get; set; }
        public IXamlMethod Getter { get; set; }
        public List<IXamlPropertySetter> Setters { get; set; } = new List<IXamlPropertySetter>();
        public List<IXamlCustomAttribute> CustomAttributes { get; set; } = new List<IXamlCustomAttribute>();
        public IXamlType DeclaringType { get; set; }
        public Dictionary<IXamlType, IXamlType> TypeConverters { get; set; } = new Dictionary<IXamlType, IXamlType>();
        
        public XamlAstClrProperty(IXamlLineInfo lineInfo, IXamlProperty property, 
            TransformerConfiguration cfg) : base(lineInfo)
        {
            Name = property.Name;
            Getter = property.Getter;
            if (property.Setter != null)
                Setters.Add(new XamlDirectCallPropertySetter(property.Setter));
            CustomAttributes = property.CustomAttributes.ToList();
            DeclaringType = (property.Getter ?? property.Setter)?.DeclaringType;
            var typeConverterAttributes = cfg.GetCustomAttribute(property, cfg.TypeMappings.TypeConverterAttributes);
            if (typeConverterAttributes != null)
            {
                foreach (var attr in typeConverterAttributes)
                {
                    var typeConverter =
                        XamlTransformHelpers.TryGetTypeConverterFromCustomAttribute(cfg, attr);
                    if (typeConverter != null)
                    {
                        TypeConverters[property.PropertyType] = typeConverter;
                        break;
                    }
                }
            }
        }

        public XamlAstClrProperty(IXamlLineInfo lineInfo, string name, IXamlType declaringType, 
            IXamlMethod getter, IEnumerable<IXamlPropertySetter> setters) : base(lineInfo)
        {
            Name = name;
            DeclaringType = declaringType;
            Getter = getter;
            if (setters != null)
                Setters.AddRange(setters);
        }

        public XamlAstClrProperty(IXamlLineInfo lineInfo, string name, IXamlType declaringType,
            IXamlMethod getter, params IXamlMethod[] setters) : this(lineInfo, name, declaringType,
            getter, setters.Select(x => new XamlDirectCallPropertySetter(x)))
        {

        }

        public override string ToString() => DeclaringType.GetFqn() + "." + Name;
    }

    class XamlDirectCallPropertySetter : IXamlPropertySetter, IXamlEmitablePropertySetter<IXamlILEmitter>
    {
        private readonly IXamlMethod _method;
        public IXamlType TargetType { get; }
        public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();
        public IReadOnlyList<IXamlType> Parameters { get; }
        public void Emit(IXamlILEmitter codegen)
        {
            codegen.EmitCall(_method, true);
        }

        public XamlDirectCallPropertySetter(IXamlMethod method)
        {
            _method = method;
            Parameters = method.ParametersWithThis().Skip(1).ToList();
            TargetType = method.ThisOrFirstParameter();
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class PropertySetterBinderParameters
    {
        public bool AllowMultiple { get; set; }
        public bool AllowXNull { get; set; } = true;
        public bool AllowRuntimeNull { get; set; } = true;
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlPropertySetter
    {
        IXamlType TargetType { get; }
        PropertySetterBinderParameters BinderParameters { get; }
        IReadOnlyList<IXamlType> Parameters { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlPropertyAssignmentNode : XamlAstNode, IXamlAstManipulationNode
    {
        public XamlAstClrProperty Property { get; }
        public List<IXamlPropertySetter> PossibleSetters { get; set; }
        public List<IXamlAstValueNode> Values { get; set; }

        public XamlPropertyAssignmentNode(IXamlLineInfo lineInfo,
            XamlAstClrProperty property,
            IEnumerable<IXamlPropertySetter> setters, IEnumerable<IXamlAstValueNode> values)
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
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlPropertyValueManipulationNode : XamlAstNode, IXamlAstManipulationNode
    {
        public XamlAstClrProperty Property { get; set; }
        public IXamlAstManipulationNode Manipulation { get; set; }
        public XamlPropertyValueManipulationNode(IXamlLineInfo lineInfo, 
            XamlAstClrProperty property, IXamlAstManipulationNode manipulation) 
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

#if !XAMLX_INTERNAL
    public
#endif
    abstract class XamlMethodCallBaseNode : XamlAstNode
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
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlNoReturnMethodCallNode : XamlMethodCallBaseNode, IXamlAstManipulationNode
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

#if !XAMLX_INTERNAL
    public
#endif
    class XamlStaticOrTargetedReturnMethodCallNode : XamlMethodCallBaseNode, IXamlAstValueNode
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

#if !XAMLX_INTERNAL
    public
#endif
    class XamlManipulationGroupNode : XamlAstNode, IXamlAstManipulationNode
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

#if !XAMLX_INTERNAL
    public
#endif
    abstract class XamlValueWithSideEffectNodeBase : XamlAstNode, IXamlAstValueNode
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
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlValueWithManipulationNode : XamlValueWithSideEffectNodeBase
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

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstNewClrObjectNode : XamlAstNode, IXamlAstValueNode
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
            Type = (IXamlAstTypeReference)Type.Visit(visitor);
            VisitList(Arguments, visitor);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstConstructableObjectNode : XamlAstNode, IXamlAstValueNode
    {
        public XamlAstConstructableObjectNode(IXamlLineInfo lineInfo,
            XamlAstClrTypeReference type, IXamlConstructor ctor,
            List<IXamlAstValueNode> arguments,
            List<IXamlAstNode> children) : base(lineInfo)
        {
            Type = type;
            Constructor = ctor;
            Arguments = arguments;
            Children = children;
        }

        public IXamlAstTypeReference Type { get; set; }
        public IXamlConstructor Constructor { get; }
        public List<IXamlAstValueNode> Arguments { get; set; } = new List<IXamlAstValueNode>();
        public List<IXamlAstNode> Children { get; set; } = new List<IXamlAstNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlAstTypeReference)Type.Visit(visitor);
            VisitList(Arguments, visitor);
            VisitList(Children, visitor);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlMarkupExtensionNode : XamlAstNode, IXamlAstValueNode, IXamlAstNodeNeedsParentStack
    {
        public IXamlAstValueNode Value { get; set; }
        public IXamlMethod ProvideValue { get; }

        public XamlMarkupExtensionNode(IXamlLineInfo lineInfo, IXamlMethod provideValue,
            IXamlAstValueNode value) : base(lineInfo)
        {
            ProvideValue = provideValue;
            Value = value;
            Type = new XamlAstClrTypeReference(this, ProvideValue.ReturnType, false);
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
        }

        public bool NeedsParentStack => ProvideValue?.Parameters.Count > 0;
        public IXamlAstTypeReference Type { get; }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlObjectInitializationNode : XamlAstNode, IXamlAstManipulationNode
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

#if !XAMLX_INTERNAL
    public
#endif
    class XamlToArrayNode : XamlAstNode, IXamlAstValueNode
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
    
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlWrappedMethod
    {
        string Name { get; }
        IXamlType ReturnType { get; }
        IXamlType DeclaringType { get; }
        IReadOnlyList<IXamlType> ParametersWithThis { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlWrappedMethod : IXamlWrappedMethod, IXamlEmitableWrappedMethod<IXamlILEmitter, XamlILNodeEmitResult>
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
        public void Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen, bool swallowResult)
        {
            codeGen.EmitCall(_method, swallowResult);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlWrappedMethodWithCasts : IXamlWrappedMethod, IXamlEmitableWrappedMethodWithLocals<IXamlILEmitter, XamlILNodeEmitResult>
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
        public void Emit(XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen, bool swallowResult)
        {
            int firstCast = -1; 
            for (var c = ParametersWithThis.Count - 1; c >= 0; c--)
            {
                if (!_method.ParametersWithThis[c].Equals(ParametersWithThis[c]))
                    firstCast = c;
            }

            if (firstCast != -1)
            {
                var locals = new Stack<XamlLocalsPool.PooledLocal>();
                for (var c = ParametersWithThis.Count - 1; c >= firstCast; c--)
                {
                    codeGen.Castclass(_method.ParametersWithThis[c]);
                    if (c > firstCast)
                    {
                        var l = context.GetLocalOfType(_method.ParametersWithThis[c]);
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

            context.Emit(_method, codeGen, swallowResult);
        }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlMethodWithCasts : IXamlCustomEmitMethod<IXamlILEmitter>
    {
        private readonly IXamlMethod _method;
        private readonly IReadOnlyList<IXamlType> _baseParametersWithThis;

        public XamlMethodWithCasts(IXamlMethod method, IEnumerable<IXamlType> newArgumentTypes)
        {
            _method = method;
            Parameters = newArgumentTypes.ToList();
            _baseParametersWithThis = _method.ParametersWithThis();
            if (_baseParametersWithThis.Count != Parameters.Count)
                throw new ArgumentException("Method argument count mismatch");
        }

        public string Name => _method.Name;
        public IXamlType ReturnType => _method.ReturnType;
        public IXamlType DeclaringType => _method.DeclaringType;
        public bool IsPublic => true;
        public bool IsStatic => true;
        public IReadOnlyList<IXamlType> Parameters { get; }
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _method.CustomAttributes;
        
        public void EmitCall(IXamlILEmitter codeGen)
        {
            int firstCast = -1; 
            for (var c = Parameters.Count - 1; c >= 0; c--)
            {
                if (!_baseParametersWithThis[c].Equals(Parameters[c]))
                    firstCast = c;
            }

            if (firstCast != -1)
            {
                var locals = new Stack<XamlLocalsPool.PooledLocal>();
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

        public bool Equals(IXamlMethod other) =>
            other is XamlMethodWithCasts mwc && mwc._method.Equals(_method) &&
            mwc.Parameters.SequenceEqual(Parameters);

        public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments)
        {
            throw new InvalidOperationException();
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlDeferredContentNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlAstValueNode Value { get; set; }
        public IXamlAstTypeReference Type { get; }
        
        public XamlDeferredContentNode(IXamlAstValueNode value, 
            TransformerConfiguration config) : base(value)
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

        void CompileBuilder(ILEmitContext context)
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
                using (var loc = context.GetLocalOfType(context.Configuration.WellKnownTypes.Object))
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

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var so = context.Configuration.WellKnownTypes.Object;
            var isp = context.Configuration.TypeMappings.ServiceProvider;
            var subType = context.CreateSubType("XamlClosure_" + context.GetNextUniqueContextId(), so);
            var buildMethod = subType.DefineMethod(so, new[]
            {
                isp
            }, "Build", true, true, false);
            CompileBuilder(new ILEmitContext(buildMethod.Generator, context.Configuration,
                context.EmitMappings, runtimeContext: context.RuntimeContext,
                contextLocal: buildMethod.Generator.DefineLocal(context.RuntimeContext.ContextType),
                createSubType: (s, type) => subType.DefineSubType(type, s, false), file: context.File,
                emitters: context.Emitters));

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
            return XamlILNodeEmitResult.Type(0, funcType);
        }
    }
#if !XAMLX_INTERNAL
    public
#endif
    class XamlDeferredContentInitializeIntermediateRootNode 
        : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlAstValueNode Value { get; set; }

        public XamlDeferredContentInitializeIntermediateRootNode(IXamlAstValueNode value) : base(value)
        {
            Value = value;
        }
        
        public override void VisitChildren(IXamlAstVisitor visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
        }

        public IXamlAstTypeReference Type => Value.Type;
        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen
                .Ldloc(context.ContextLocal);
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            codeGen
                .Stfld(context.RuntimeContext.IntermediateRootObjectField)
                .Ldloc(context.ContextLocal)
                .Ldfld(context.RuntimeContext.IntermediateRootObjectField);
            return XamlILNodeEmitResult.Type(0, Value.Type.GetClrType());
        }
    }
}
