using System;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using XamlIl.Transform;
using XamlIl.TypeSystem;
using Visitor = XamlIl.Ast.IXamlIlAstVisitor;
namespace XamlIl.Ast
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlNullExtensionNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlNullExtensionNode(IXamlIlLineInfo lineInfo) : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(lineInfo, XamlIlPseudoType.Null, false);
        }

        public IXamlIlAstTypeReference Type { get; }
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            codeGen.Emit(OpCodes.Ldnull);
            return XamlIlNodeEmitResult.Type(0, XamlIlPseudoType.Null);
        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlTypeExtensionNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        private readonly IXamlIlType _systemType;

        public XamlIlTypeExtensionNode(IXamlIlLineInfo lineInfo, IXamlIlAstTypeReference value,
            IXamlIlType systemType) : base(lineInfo)
        {
            _systemType = systemType;
            Type = new XamlIlAstClrTypeReference(this, systemType, false);
            Value = value;
        }

        public IXamlIlAstTypeReference Type { get; }
        public IXamlIlAstTypeReference Value { get; set; }

        public override void VisitChildren(Visitor visitor)
        {
            Value = Value.Visit(visitor) as IXamlIlAstTypeReference;
        }

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            var type = Value.GetClrType();
            var method = _systemType.Methods.FirstOrDefault(m =>
                m.Name == "GetTypeFromHandle" && m.Parameters.Count == 1 &&
                m.Parameters[0].Name == "RuntimeTypeHandle");

            if (method == null)
                throw new XamlIlTypeSystemException(
                    $"Unable to find GetTypeFromHandle(RuntimeTypeHandle) on {_systemType.GetFqn()}");
            codeGen
                .Emit(OpCodes.Ldtoken, type)
                .EmitCall(method);
            return XamlIlNodeEmitResult.Type(0, _systemType);
        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlStaticExtensionNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlStaticExtensionNode(XamlIlAstObjectNode lineInfo, IXamlIlAstTypeReference targetType, string member) : base(lineInfo)
        {
            TargetType = targetType;
            Member = member;
        }

        public string Member { get; set; }
        public IXamlIlAstTypeReference TargetType { get; set; }

        public override void VisitChildren(Visitor visitor)
        {
            TargetType = (IXamlIlAstTypeReference) TargetType.Visit(visitor);
        }

        IXamlIlMember ResolveMember(IXamlIlType type)
        {
            var rv = type.Fields.FirstOrDefault(f => f.IsPublic && f.IsStatic && f.Name == Member) ??
                   (IXamlIlMember) type.GetAllProperties().FirstOrDefault(p =>
                       p.Name == Member && p.Getter != null && p.Getter.IsPublic && p.Getter.IsStatic);
            if (rv == null)
                throw new XamlIlParseException(
                    $"Unable to resolve {Member} as static field, property, constant or enum value", this);
            return rv;
        }
        
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            var type = TargetType.GetClrType();
            var member = ResolveMember(type);
            if (member is IXamlIlProperty prop)
            {
                codeGen.Emit(OpCodes.Call, prop.Getter);
                return XamlIlNodeEmitResult.Type(0, prop.Getter.ReturnType);
            }
            else if (member is IXamlIlField field)
            {
                if (field.IsLiteral)
                {
                    TypeSystemHelpers.EmitFieldLiteral(field, codeGen);
                }
                else
                    codeGen.Emit(OpCodes.Ldsfld, field);
                return XamlIlNodeEmitResult.Type(0, field.FieldType);
            }
            else
                throw new XamlIlLoadException(
                    $"Unable to resolve {Member} as static field, property, constant or enum value", this);
        }

        IXamlIlType ResolveReturnType()
        {
            if (!(TargetType is XamlIlAstClrTypeReference type))
                return XamlIlPseudoType.Unknown;
            var member = ResolveMember(type.Type);
            if (member is IXamlIlField field)
                return field.FieldType;
            if (member is IXamlIlProperty prop && prop.Getter != null)
                return prop.Getter.ReturnType;
            throw new XamlIlParseException($"Unable to resolve {Member} as static field, property, constant or enum value", this);
        }
        
        public IXamlIlAstTypeReference Type => new XamlIlAstClrTypeReference(this, ResolveReturnType(), false);
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlConstantNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public object Constant { get; }

        public XamlIlConstantNode(IXamlIlLineInfo lineInfo, IXamlIlType type, object constant) : base(lineInfo)
        {
            if (!constant.GetType().IsPrimitive)
                throw new ArgumentException($"Don't know how to emit {constant.GetType()} constant");
            Constant = constant;
            Type = new XamlIlAstClrTypeReference(lineInfo, type, false);

        }

        public IXamlIlAstTypeReference Type { get; }
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (Constant is string)
                codeGen.Emit(OpCodes.Ldstr, (string) Constant);
            else if (Constant is long || Constant is ulong)
                codeGen.Emit(OpCodes.Ldc_I8, TypeSystemHelpers.ConvertLiteralToLong(Constant));
            else if (Constant is float f)
                codeGen.Emit(OpCodes.Ldc_R4, f);
            else if (Constant is double d)
                codeGen.Emit(OpCodes.Ldc_R8, d);
            else
                codeGen.Emit(OpCodes.Ldc_I4, TypeSystemHelpers.ConvertLiteralToInt(Constant));
            return XamlIlNodeEmitResult.Type(0, Type.GetClrType());
        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlRootObjectNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlRootObjectNode(XamlIlAstObjectNode root) : base(root)
        {
            Type = root.Type;
        }

        public IXamlIlAstTypeReference Type { get; set; }

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            codeGen
                .Ldloc(context.ContextLocal)
                .Ldfld(context.RuntimeContext.RootObjectField);
            return XamlIlNodeEmitResult.Type(0, Type.GetClrType());
        }

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlIlAstTypeReference) Type.Visit(visitor);
        }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlIntermediateRootObjectNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlIntermediateRootObjectNode(IXamlIlLineInfo lineInfo, XamlIlTypeWellKnownTypes types) : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(lineInfo, types.Object, false);
        }

        public IXamlIlAstTypeReference Type { get; set; }

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            codeGen
                .Ldloc(context.ContextLocal)
                .Ldfld(context.RuntimeContext.IntermediateRootObjectField);
            return XamlIlNodeEmitResult.Type(0, Type.GetClrType());
        }

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlIlAstTypeReference) Type.Visit(visitor);
        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlLoadMethodDelegateNode : XamlIlValueWithSideEffectNodeBase,
        IXamlIlAstEmitableNode
    {
        public IXamlIlType DelegateType { get; }
        public IXamlIlMethod Method { get; }

        public XamlIlLoadMethodDelegateNode(IXamlIlLineInfo lineInfo, IXamlIlAstValueNode value,
            IXamlIlType delegateType, IXamlIlMethod method) : base(lineInfo, value)
        {
            DelegateType = delegateType;
            Method = method;
            Type = new XamlIlAstClrTypeReference(value, DelegateType, false);
        }

        public override IXamlIlAstTypeReference Type { get; }
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            context.Emit(Value, codeGen, Method.DeclaringType);
            codeGen
                .Ldftn(Method)
                .Newobj(DelegateType.Constructors.FirstOrDefault(ct =>
                    ct.Parameters.Count == 2 && ct.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));
            return XamlIlNodeEmitResult.Type(0, DelegateType);
        }
    }
}
