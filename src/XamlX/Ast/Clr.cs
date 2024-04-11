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
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsFamily { get; set; }
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
            var accessor = property.Getter ?? property.Setter;
            DeclaringType = accessor?.DeclaringType;
            IsPrivate = accessor?.IsPrivate == true;
            IsPublic = accessor?.IsPublic == true;
            IsFamily = accessor?.IsFamily == true;
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
            IsPublic = getter?.IsPublic == true;
            IsPrivate = getter?.IsPrivate == true;
            IsFamily = getter?.IsFamily == true;
            if (setters != null)
                Setters.AddRange(setters);
        }

        public XamlAstClrProperty(IXamlLineInfo lineInfo, string name, IXamlType declaringType,
            IXamlMethod getter, params IXamlMethod[] setters) : this(lineInfo, name, declaringType,
            getter, setters.Where(x=> !(x is null)).Select(x => new XamlDirectCallPropertySetter(x)))
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
        public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();
        public IReadOnlyList<IXamlType> Parameters { get; }
        
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

            bool allowNull = Parameters.Last().AcceptsNull();
            BinderParameters = new PropertySetterBinderParameters
            {
                AllowMultiple = false,
                AllowXNull = allowNull,
                AllowRuntimeNull = allowNull
            };
        }

        public bool Equals(XamlDirectCallPropertySetter other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return _method.Equals(other._method) && BinderParameters.Equals(other.BinderParameters);
        }

        public override bool Equals(object obj)
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

        public bool Equals(PropertySetterBinderParameters other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            
            return AllowMultiple == other.AllowMultiple 
                   && AllowXNull == other.AllowXNull
                   && AllowRuntimeNull == other.AllowRuntimeNull;
        }

        public override bool Equals(object obj) 
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
        private readonly IXamlType _deferredContentCustomizationTypeParameter;
        public IXamlAstValueNode Value { get; set; }
        public IXamlAstTypeReference Type { get; }
        
        public XamlDeferredContentNode(IXamlAstValueNode value,
            IXamlType deferredContentCustomizationTypeParameter,
            TransformerConfiguration config) : base(value)
        {
            _deferredContentCustomizationTypeParameter = deferredContentCustomizationTypeParameter;
            Value = value;
            var funcType = config.TypeSystem.GetType("System.Func`2")
                .MakeGenericType(config.TypeMappings.ServiceProvider, config.WellKnownTypes.Object);
            Type = new XamlAstClrTypeReference(value, funcType, false);
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

            if (!context.TryGetItem(out XamlClosureInfo closureInfo))
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

            var funcType = Type.GetClrType();
            codeGen
                .Ldnull()
                .Ldftn(buildMethod)
                .Newobj(funcType.Constructors.FirstOrDefault(ct =>
                    ct.Parameters.Count == 2 && ct.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));

            // Allow to save values from the parent context, pass own service provider, etc, etc
            if (context.Configuration.TypeMappings.DeferredContentExecutorCustomization != null)
            {

                var customization = context.Configuration.TypeMappings.DeferredContentExecutorCustomization;
                if (_deferredContentCustomizationTypeParameter != null)
                    customization =
                        customization.MakeGenericMethod(new[] { _deferredContentCustomizationTypeParameter });
                codeGen
                    .Ldloc(context.ContextLocal)
                    .EmitCall(customization);
            }

            return XamlILNodeEmitResult.Type(0, funcType);
        }

#nullable enable

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
                            .FindMethod(m => m.Name == "GetService"))
                        .Stloc(loc.Local)
                        // if(loc == null) goto noRoot;
                        .Ldloc(loc.Local)
                        .Brfalse(noRoot)
                        // loc = ((IRootObjectProvider)loc).RootObject
                        .Ldloc(loc.Local)
                        .Castclass(rootObjectProviderType)
                        .EmitCall(rootObjectProviderType
                            .FindMethod(m => m.Name == "get_RootObject"))
                        .Stloc(loc.Local)
                        // contextLocal.RootObject = loc;
                        .Ldloc(context.ContextLocal)
                        .Ldloc(loc.Local)
                        .Castclass(context.RuntimeContext.ContextType.GenericArguments[0])
                        .Stfld(context.RuntimeContext.RootObjectField)
                        .MarkLabel(noRoot)
                        .Ldloc(context.ContextLocal);
                }

                il.Ret();

                return method;
            }
        }

#nullable restore
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
