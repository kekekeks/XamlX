using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
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
    class XamlNullExtensionNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlNullExtensionNode(IXamlLineInfo lineInfo) : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(lineInfo, XamlPseudoType.Null, false);
        }

        public IXamlAstTypeReference Type { get; }
        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Emit(OpCodes.Ldnull);
            return XamlILNodeEmitResult.Type(0, XamlPseudoType.Null);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlTypeExtensionNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        private readonly IXamlType _systemType;

        public XamlTypeExtensionNode(IXamlLineInfo lineInfo, IXamlAstTypeReference value,
            IXamlType systemType) : base(lineInfo)
        {
            _systemType = systemType;
            Type = new XamlAstClrTypeReference(this, systemType, false);
            Value = value;
        }

        public IXamlAstTypeReference Type { get; }
        public IXamlAstTypeReference Value { get; set; }

        public override void VisitChildren(Visitor visitor)
        {
            Value = (IXamlAstTypeReference)Value.Visit(visitor);
        }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var type = Value.GetClrType();
            var method = _systemType.Methods.FirstOrDefault(m =>
                m.Name == "GetTypeFromHandle" && m.Parameters.Count == 1 &&
                m.Parameters[0].Name == "RuntimeTypeHandle");

            if (method == null)
                throw new XamlTypeSystemException(
                    $"Unable to find GetTypeFromHandle(RuntimeTypeHandle) on {_systemType.GetFqn()}");
            codeGen
                .Emit(OpCodes.Ldtoken, type)
                .EmitCall(method);
            return XamlILNodeEmitResult.Type(0, _systemType);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlStaticExtensionNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlStaticExtensionNode(XamlAstObjectNode lineInfo, IXamlAstTypeReference? targetType, string member) : base(lineInfo)
        {
            TargetType = targetType;
            Member = member;
        }

        public string Member { get; set; }
        public IXamlAstTypeReference? TargetType { get; set; }

        public override void VisitChildren(Visitor visitor)
        {
            TargetType = (IXamlAstTypeReference?) TargetType?.Visit(visitor);
        }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var member = ResolveMember(true);
            switch (member)
            {
                case IXamlProperty prop:
                    var getter = prop.Getter ?? throw new InvalidOperationException($"Property {prop} doesn't have a getter");
                    codeGen.Emit(OpCodes.Call, getter);
                    return XamlILNodeEmitResult.Type(0, getter.ReturnType);
                case IXamlField field:
                {
                    if (field.IsLiteral)
                    {
                        ILEmitHelpers.EmitFieldLiteral(field, codeGen);
                    }
                    else
                        codeGen.Emit(OpCodes.Ldsfld, field);
                    return XamlILNodeEmitResult.Type(0, field.FieldType);
                }
                default:
                    throw new InvalidOperationException();
            }
        }

        internal IXamlMember? ResolveMember(bool throwOnUnknown)
        {
            var type = TargetType?.GetClrType();
            var member = type?.Fields.FirstOrDefault(f => f.IsPublic && f.IsStatic && f.Name == Member) ??
                   (IXamlMember?)type?.GetAllProperties().FirstOrDefault(p =>
                       p.Name == Member && p.Getter is { IsPublic: true, IsStatic: true });

            if (member is IXamlProperty or IXamlField)
            {
                return member;
            }
            else if (throwOnUnknown)
            {
                throw new XamlTransformException(
                    $"Unable to resolve \"{type?.Name}.{Member}\" as static field, property, constant or enum value", this);
            }
            return null;
        }

        public IXamlAstTypeReference Type => new XamlAstClrTypeReference(this, ResolveMember(false) switch
        {
            IXamlField field => field.FieldType,
            IXamlProperty { Getter: not null } prop => prop.Getter.ReturnType,
            _ => XamlPseudoType.Unknown
        }, false);
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlConstantNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public object Constant { get; }

        public XamlConstantNode(IXamlLineInfo lineInfo, IXamlType type, object constant) : base(lineInfo)
        {
            if (!constant.GetType().IsPrimitive && constant.GetType() != typeof(string))
                throw new ArgumentException($"Don't know how to emit {constant.GetType()} constant");
            Constant = constant;
            Type = new XamlAstClrTypeReference(lineInfo, type, false);
        }

        public IXamlAstTypeReference Type { get; }
        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (Constant is string)
                codeGen.Emit(OpCodes.Ldstr, (string) Constant);
            else if (Constant is long || Constant is ulong)
                codeGen.Emit(OpCodes.Ldc_I8, TypeSystem.TypeSystemHelpers.ConvertLiteralToLong(Constant));
            else if (Constant is float f)
                codeGen.Emit(OpCodes.Ldc_R4, f);
            else if (Constant is double d)
                codeGen.Emit(OpCodes.Ldc_R8, d);
            else if (Constant is bool b)
                codeGen.Emit(b ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            else
                codeGen.Ldc_I4(TypeSystem.TypeSystemHelpers.ConvertLiteralToInt(Constant));
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlRootObjectNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        private IXamlAstTypeReference _type;

        public XamlRootObjectNode(XamlAstObjectNode root) : base(root)
        {
            _type = root.Type ?? throw new InvalidOperationException("XamlRootObjectNode.Type cannot be null.");
        }

        public IXamlAstTypeReference Type
        {
            get => _type;
            set => _type = value ?? throw new InvalidOperationException("XamlAstObjectNode.Type cannot be null.");
        }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            Debug.Assert(context.RuntimeContext.RootObjectField is not null);
            var rootObjectField = context.RuntimeContext.RootObjectField;

            codeGen
                .Ldloc(context.ContextLocal)
                .Ldfld(rootObjectField!);
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlAstTypeReference) Type.Visit(visitor);
        }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlIntermediateRootObjectNode : XamlAstNode, IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlIntermediateRootObjectNode(IXamlLineInfo lineInfo, XamlTypeWellKnownTypes types) : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(lineInfo, types.Object, false);
        }

        public IXamlAstTypeReference Type { get; set; }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            Debug.Assert(context.RuntimeContext.IntermediateRootObjectField is not null);
            var intermediateRootObjectField = context.RuntimeContext.IntermediateRootObjectField!;

            codeGen
                .Ldloc(context.ContextLocal)
                .Ldfld(intermediateRootObjectField);
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlAstTypeReference) Type.Visit(visitor);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlLoadMethodDelegateNode : XamlValueWithSideEffectNodeBase,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlType DelegateType { get; }
        public IXamlMethod Method { get; }

        public XamlLoadMethodDelegateNode(IXamlLineInfo lineInfo, IXamlAstValueNode value,
            IXamlType delegateType, IXamlMethod method) : base(lineInfo, value)
        {
            DelegateType = delegateType;
            Method = method;
            Type = new XamlAstClrTypeReference(value, DelegateType, false);
        }

        public override IXamlAstTypeReference Type { get; }
        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            context.Emit(Value, codeGen, Method.DeclaringType);
            codeGen
                .Ldftn(Method)
                .Newobj(DelegateType.Constructors.First(ct =>
                    ct.Parameters.Count == 2 && ct.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));
            return XamlILNodeEmitResult.Type(0, DelegateType);
        }
    }
}
