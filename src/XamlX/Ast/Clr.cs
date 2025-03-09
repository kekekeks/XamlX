using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsFamily { get; set; }
        public IXamlMethod? Getter { get; set; }
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
            var accessor = property.Getter ?? property.Setter;
            DeclaringType = property.DeclaringType;
            IsPrivate = accessor?.IsPrivate == true;
            IsPublic = accessor?.IsPublic == true;
            IsFamily = accessor?.IsFamily == true;
            var typeConverterAttributes = cfg.GetCustomAttribute(property, cfg.TypeMappings.TypeConverterAttributes);
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

        public XamlAstClrProperty(
            IXamlLineInfo lineInfo,
            string name,
            IXamlType declaringType,
            IXamlMethod? getter,
            IEnumerable<IXamlPropertySetter>? setters,
            IEnumerable<IXamlCustomAttribute>? customAttributes)
            : base(lineInfo)
        {
            Name = name;
            DeclaringType = declaringType;
            Getter = getter;
            IsPublic = getter?.IsPublic == true;
            IsPrivate = getter?.IsPrivate == true;
            IsFamily = getter?.IsFamily == true;
            if (setters != null)
                Setters.AddRange(setters);
            if (customAttributes is not null)
                CustomAttributes.AddRange(customAttributes);
        }

        public XamlAstClrProperty(
            IXamlLineInfo lineInfo,
            string name,
            IXamlType declaringType,
            IXamlMethod? getter,
            IEnumerable<IXamlMethod?>? setters,
            IEnumerable<IXamlCustomAttribute>? customAttributes)
            : this(
                lineInfo,
                name,
                declaringType,
                getter,
                setters?.Where(x => x is not null).Select(x => new XamlDirectCallPropertySetter(x!)),
                customAttributes)
        {
        }

        public XamlAstClrProperty(IXamlLineInfo lineInfo, string name, IXamlType declaringType, IXamlMethod? getter)
            : this(lineInfo, name, declaringType, getter, (IEnumerable<IXamlPropertySetter>?)null, null)
        {
        }

        public override string ToString() => DeclaringType.GetFqn() + "." + Name;
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlILOptimizedEmitablePropertySetter : IXamlEmitablePropertySetter<IXamlILEmitter>
    {
        void EmitWithArguments(
            XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter emitter,
            IReadOnlyList<IXamlAstValueNode> arguments);
    }

    class XamlDirectCallPropertySetter : IXamlILOptimizedEmitablePropertySetter, IEquatable<XamlDirectCallPropertySetter>
    {
        private readonly IXamlMethod _method;
        public IXamlType TargetType { get; }
        public PropertySetterBinderParameters BinderParameters { get; }
        public IReadOnlyList<IXamlType> Parameters { get; }
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _method.CustomAttributes;

        public void Emit(IXamlILEmitter emitter)
            => emitter.EmitCall(_method, true);

        public void EmitWithArguments(
            XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter emitter,
            IReadOnlyList<IXamlAstValueNode> arguments)
        {
            for (var i = 0; i < arguments.Count; ++i)
                context.Emit(arguments[i], emitter, Parameters[i]);

            emitter.EmitCall(_method, true);
        }

        public XamlDirectCallPropertySetter(IXamlMethod method)
        {
            _method = method;
            Parameters = method.ParametersWithThis().Skip(1).ToList();
            TargetType = method.ThisOrFirstParameter();

            bool allowNull = Parameters[Parameters.Count - 1].AcceptsNull();
            BinderParameters = new PropertySetterBinderParameters
            {
                AllowMultiple = false,
                AllowXNull = allowNull,
                AllowRuntimeNull = allowNull
            };
        }

        public bool Equals(XamlDirectCallPropertySetter? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return _method.Equals(other._method) && BinderParameters.Equals(other.BinderParameters);
        }

        public override bool Equals(object? obj)
            => Equals(obj as XamlDirectCallPropertySetter);

        public override int GetHashCode() 
            => (_method.GetHashCode() * 397) ^ BinderParameters.GetHashCode();
    }

#if !XAMLX_INTERNAL
    public
#endif
    class PropertySetterBinderParameters : IEquatable<PropertySetterBinderParameters>
    {
        public bool AllowMultiple { get; set; }
        public bool AllowXNull { get; set; } = true;
        public bool AllowRuntimeNull { get; set; } = true;
        public bool AllowAttributeSyntax { get; set; } = true;

        public bool Equals(PropertySetterBinderParameters? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            
            return AllowMultiple == other.AllowMultiple 
                   && AllowXNull == other.AllowXNull
                   && AllowRuntimeNull == other.AllowRuntimeNull;
        }

        public override bool Equals(object? obj)
            => Equals(obj as PropertySetterBinderParameters);

        public override int GetHashCode()
        {
            int hashCode = AllowMultiple.GetHashCode();
            hashCode = (hashCode * 397) ^ AllowXNull.GetHashCode();
            hashCode = (hashCode * 397) ^ AllowRuntimeNull.GetHashCode();
            return hashCode;
        }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlPropertySetter
    {
        IXamlType TargetType { get; }
        PropertySetterBinderParameters BinderParameters { get; }
        IReadOnlyList<IXamlType> Parameters { get; }
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
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
            IXamlWrappedMethod method, IEnumerable<IXamlAstValueNode>? args)
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
            IEnumerable<IXamlAstValueNode>? args)
            : base(lineInfo, method, args)
        {
            Type = new XamlAstClrTypeReference(lineInfo, method.ReturnType, false);
        }

        public XamlStaticOrTargetedReturnMethodCallNode(IXamlLineInfo lineInfo, IXamlMethod method,
            IEnumerable<IXamlAstValueNode>? args)
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
            IEnumerable<IXamlAstManipulationNode>? children = null)
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
        public IXamlAstManipulationNode? Manipulation { get; set; }

        public XamlValueWithManipulationNode(IXamlLineInfo lineInfo,
            IXamlAstValueNode value,
            IXamlAstManipulationNode? manipulation) : base(lineInfo, value)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(Visitor visitor)
        {
            base.VisitChildren(visitor);
            Manipulation = (IXamlAstManipulationNode?)Manipulation?.Visit(visitor);
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
        public List<IXamlAstValueNode> Arguments { get; set; }

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
        public List<IXamlAstValueNode> Arguments { get; set; }
        public List<IXamlAstNode> Children { get; set; }

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

        public bool NeedsParentStack => ProvideValue.Parameters.Count > 0;
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
            ParametersWithThis = method.ParametersWithThis();
            ReturnType = method.ReturnType;
        }

        public string Name => _method.Name;
        public IXamlType ReturnType { get; }
        public IXamlType DeclaringType => _method.DeclaringType;
        public IReadOnlyList<IXamlType> ParametersWithThis { get; }
        public void Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen, bool swallowResult)
        {
            codeGen.EmitCall(_method, context, swallowResult);
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
        public bool IsPrivate => false;
        public bool IsFamily => false;
        public bool IsStatic => true;
        public IReadOnlyList<IXamlType> Parameters { get; }
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _method.CustomAttributes;
        public IXamlParameterInfo GetParameterInfo(int index) => _method.GetParameterInfo(index);

        public bool IsGenericMethodDefinition => _method.IsGenericMethodDefinition;

        public IReadOnlyList<IXamlType> GenericParameters => _method.GenericParameters;

        public IReadOnlyList<IXamlType> GenericArguments => _method.GenericArguments;

        public bool IsGenericMethod => _method.IsGenericMethod;

        public bool ContainsGenericParameters => _method.ContainsGenericParameters;

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

        public bool Equals(IXamlMethod? other) =>
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
        private readonly IXamlMethod? _deferredContentCustomization;
        private readonly IXamlType? _deferredContentCustomizationTypeParameter;
        private readonly IXamlType _funcType;

        public IXamlAstValueNode Value { get; set; }
        public IXamlAstTypeReference Type { get; }
        
        public XamlDeferredContentNode(IXamlAstValueNode value,
            IXamlType? deferredContentCustomizationTypeParameter,
            TransformerConfiguration config) : base(value)
        {
            _deferredContentCustomization = config.TypeMappings.DeferredContentExecutorCustomization;
            _deferredContentCustomizationTypeParameter = deferredContentCustomizationTypeParameter;
            Value = value;

            _funcType = config.TypeSystem
                .GetType("System.Func`2")
                .MakeGenericType(config.TypeMappings.ServiceProvider, config.WellKnownTypes.Object);

            var returnType = _deferredContentCustomization?.ReturnType ?? _funcType;
            Type = new XamlAstClrTypeReference(value, returnType, false);
        }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlAstValueNode) Value.Visit(visitor);
        }

        void CompileBuilder(ILEmitContext context, XamlClosureInfo xamlClosure)
        {
            var il = context.Emitter;
            // Initialize the context
            il
                .Ldarg_0()
                .EmitCall(xamlClosure.CreateRuntimeContextMethod)
                .Stloc(context.ContextLocal);

            context.Emit(Value, context.Emitter, context.Configuration.WellKnownTypes.Object);
            il.Ret();

            context.ExecuteAfterEmitCallbacks();
        }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var so = context.Configuration.WellKnownTypes.Object;
            var isp = context.Configuration.TypeMappings.ServiceProvider;

            if (!context.TryGetItem(out XamlClosureInfo? closureInfo))
            {
                var closureType = context.DeclaringType.DefineSubType(
                    so,
                    "XamlClosure_" + context.Configuration.IdentifierGenerator.GenerateIdentifierPart(),
                    XamlVisibility.Private);

                closureInfo = new XamlClosureInfo(closureType, context);
                context.AddAfterEmitCallbacks(() => closureType.CreateType());
                context.SetItem(closureInfo);
            }

            var counter = ++closureInfo.BuildMethodCounter;

            var buildMethod = closureInfo.Type.DefineMethod(
                so,
                new[] { isp },
                $"Build_{counter}",
                XamlVisibility.Public,
                true,
                false);

            var subContext = new ILEmitContext(
                buildMethod.Generator,
                context.Configuration,
                context.EmitMappings,
                context.RuntimeContext,
                buildMethod.Generator.DefineLocal(context.RuntimeContext.ContextType),
                closureInfo.Type,
                context.File,
                context.Emitters);

            subContext.SetItem(closureInfo);

            CompileBuilder(subContext, closureInfo);

            var customization = _deferredContentCustomization;

            if (_deferredContentCustomizationTypeParameter is not null)
                customization = customization?.MakeGenericMethod(new[] { _deferredContentCustomizationTypeParameter });

            if (customization is not null && IsFunctionPointerLike(customization.Parameters[0]))
            {
                // &Build
                codeGen
                    .Ldftn(buildMethod);
            }
            else
            {
                // new Func<IServiceProvider, object>(null, &Build);
                codeGen
                    .Ldnull()
                    .Ldftn(buildMethod)
                    .Newobj(_funcType.Constructors.First(ct =>
                        ct.Parameters.Count == 2 &&
                        ct.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));
            }

            // Allow to save values from the parent context, pass own service provider, etc, etc
            if (customization is not null)
            {
                codeGen
                    .Ldloc(context.ContextLocal)
                    .EmitCall(customization);
            }

            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }

        private static bool IsFunctionPointerLike(IXamlType xamlType)
            => xamlType.IsFunctionPointer // Cecil, SRE with .NET 8
               || xamlType.FullName == "System.IntPtr"; // SRE with .NET < 8 or .NET Standard

        private sealed class XamlClosureInfo
        {
            private readonly XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> _parentContext;
            private IXamlMethod? _createRuntimeContextMethod;

            public IXamlTypeBuilder<IXamlILEmitter> Type { get; }

            public IXamlMethod CreateRuntimeContextMethod
                => _createRuntimeContextMethod ??= BuildCreateRuntimeContextMethod();

            public int BuildMethodCounter { get; set; }

            public XamlClosureInfo(
                IXamlTypeBuilder<IXamlILEmitter> type,
                XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> parentContext)
            {
                Type = type;
                _parentContext = parentContext;
            }

            private IXamlMethod BuildCreateRuntimeContextMethod()
            {
                var method = Type.DefineMethod(
                    _parentContext.RuntimeContext.ContextType,
                    new[] { _parentContext.Configuration.TypeMappings.ServiceProvider },
                    "CreateContext",
                    XamlVisibility.Public,
                    true,
                    false);

                var context = new ILEmitContext(
                    method.Generator,
                    _parentContext.Configuration,
                    _parentContext.EmitMappings,
                    _parentContext.RuntimeContext,
                    method.Generator.DefineLocal(_parentContext.RuntimeContext.ContextType),
                    Type,
                    _parentContext.File,
                    _parentContext.Emitters);

                var il = context.Emitter;

                // context = new Context(arg0, ...)
                il.Ldarg_0();
                context.RuntimeContext.Factory(il);

                if (context.Configuration.TypeMappings.RootObjectProvider is { } rootObjectProviderType)
                {
                    // Attempt to get the root object from parent service provider
                    var noRoot = il.DefineLabel();
                    using var loc = context.GetLocalOfType(context.Configuration.WellKnownTypes.Object);
                    il
                        .Stloc(context.ContextLocal)
                        // if(arg == null) goto noRoot;
                        .Ldarg_0()
                        .Brfalse(noRoot)
                        // var loc = arg.GetService(typeof(IRootObjectProvider))
                        .Ldarg_0()
                        .Ldtype(rootObjectProviderType)
                        .EmitCall(context.Configuration.TypeMappings.ServiceProvider
                            .GetMethod(m => m.Name == "GetService"))
                        .Stloc(loc.Local)
                        // if(loc == null) goto noRoot;
                        .Ldloc(loc.Local)
                        .Brfalse(noRoot)
                        // loc = ((IRootObjectProvider)loc).RootObject
                        .Ldloc(loc.Local)
                        .Castclass(rootObjectProviderType)
                        .EmitCall(rootObjectProviderType
                            .GetMethod(m => m.Name == "get_RootObject"))
                        .Stloc(loc.Local)
                        // contextLocal.RootObject = loc;
                        .Ldloc(context.ContextLocal)
                        .Ldloc(loc.Local)
                        .Castclass(context.RuntimeContext.ContextType.GenericArguments[0])
                        .Stfld(context.RuntimeContext.RootObjectField!)
                        .MarkLabel(noRoot)
                        .Ldloc(context.ContextLocal);
                }

                il.Ret();

                return method;
            }
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
            Debug.Assert(context.RuntimeContext.IntermediateRootObjectField is not null);
            var intermediateRootObjectField = context.RuntimeContext.IntermediateRootObjectField!;

            codeGen
                .Ldloc(context.ContextLocal);
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            codeGen
                .Stfld(intermediateRootObjectField)
                .Ldloc(context.ContextLocal)
                .Ldfld(intermediateRootObjectField);
            return XamlILNodeEmitResult.Type(0, Value.Type.GetClrType());
        }
    }
}
