using System;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.Ast
{
    public class XamlXNullExtensionNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public XamlXNullExtensionNode(IXamlXLineInfo lineInfo) : base(lineInfo)
        {
            Type = new XamlXAstClrTypeReference(lineInfo, XamlXPseudoType.Null);
        }

        public IXamlXAstTypeReference Type { get; }
        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            codeGen.Emit(OpCodes.Ldnull);
            return XamlXNodeEmitResult.Type(0, XamlXPseudoType.Null);
        }
    }

    public class XamlXTypeExtensionNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        private readonly IXamlXType _systemType;

        public XamlXTypeExtensionNode(IXamlXLineInfo lineInfo, IXamlXAstTypeReference value,
            IXamlXType systemType) : base(lineInfo)
        {
            _systemType = systemType;
            Type = new XamlXAstClrTypeReference(this, systemType);
            Value = value;
        }

        public IXamlXAstTypeReference Type { get; }
        public IXamlXAstTypeReference Value { get; set; }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Value = Value.Visit(visitor) as IXamlXAstTypeReference;
        }

        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            var type = Value.GetClrType();
            var method = _systemType.Methods.FirstOrDefault(m =>
                m.Name == "GetTypeFromHandle" && m.Parameters.Count == 1 &&
                m.Parameters[0].Name == "RuntimeTypeHandle");

            if (method == null)
                throw new XamlXTypeSystemException(
                    $"Unable to find GetTypeFromHandle(RuntimeTypeHandle) on {_systemType.GetFqn()}");
            codeGen
                .Emit(OpCodes.Ldtoken, type)
                .Emit(OpCodes.Call, method);
            return XamlXNodeEmitResult.Type(0, _systemType);
        }
    }

    public class XamlXStaticExtensionNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public XamlXStaticExtensionNode(XamlXAstObjectNode lineInfo, IXamlXAstTypeReference targetType, string member) : base(lineInfo)
        {
            TargetType = targetType;
            Member = member;
        }

        public string Member { get; set; }
        public IXamlXAstTypeReference TargetType { get; set; }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            TargetType = (IXamlXAstTypeReference) TargetType.Visit(visitor);
        }

        IXamlXMember ResolveMember(IXamlXType type)
        {
            return type.Fields.FirstOrDefault(f => f.IsPublic && f.IsStatic && f.Name == Member) ??
                   (IXamlXMember) type.GetAllProperties().FirstOrDefault(p =>
                       p.Name == Member && p.Getter != null && p.Getter.IsPublic && p.Getter.IsStatic);
        }
        
        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            var type = TargetType.GetClrType();
            var member = ResolveMember(type);
            if (member is IXamlXProperty prop)
            {
                codeGen.Emit(OpCodes.Call, prop.Getter);
                return XamlXNodeEmitResult.Type(0, prop.Getter.ReturnType);
            }
            else if (member is IXamlXField field)
            {
                if (field.IsLiteral)
                {
                    TypeSystemHelpers.EmitFieldLiteral(field, codeGen);
                }
                else
                    codeGen.Emit(OpCodes.Ldsfld, field);
                return XamlXNodeEmitResult.Type(0, field.FieldType);
            }
            else
                throw new XamlXLoadException(
                    $"Unable to resolve {Member} as static field, property, constant or enum value", this);
        }

        IXamlXType ResolveReturnType()
        {
            if (!(TargetType is XamlXAstClrTypeReference type))
                return XamlXPseudoType.Unknown;
            var member = ResolveMember(type.Type);
            if (member is IXamlXField field)
                return field.FieldType;
            if (member is IXamlXProperty prop && prop.Getter != null)
                return prop.Getter.ReturnType;
            return XamlXPseudoType.Unknown;
        }
        
        public IXamlXAstTypeReference Type => new XamlXAstClrTypeReference(this, ResolveReturnType());
    }

    public class XamlXConstantNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public object Constant { get; }

        public XamlXConstantNode(IXamlXLineInfo lineInfo, IXamlXType type, object constant) : base(lineInfo)
        {
            if (!constant.GetType().IsPrimitive)
                throw new ArgumentException($"Don't know how to emit {constant.GetType()} constant");
            Constant = constant;
            Type = new XamlXAstClrTypeReference(lineInfo, type);

        }

        public IXamlXAstTypeReference Type { get; }
        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXEmitter codeGen)
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
            return XamlXNodeEmitResult.Type(0, Type.GetClrType());
        }
    }

    public class XamlXRootObjectNode : XamlXAstNode, IXamlXAstValueNode, IXamlXAstEmitableNode
    {
        public XamlXRootObjectNode(XamlXAstObjectNode root) : base(root)
        {
            Type = root.Type;
        }

        public IXamlXAstTypeReference Type { get; set; }

        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            codeGen
                .Ldloc(context.ContextLocal)
                .Ldfld(context.RuntimeContext.RootObjectField);
            return XamlXNodeEmitResult.Type(0, Type.GetClrType());
        }

        public override void VisitChildren(XamlXAstVisitorDelegate visitor)
        {
            Type = (IXamlXAstTypeReference) Type.Visit(visitor);
        }
    }

    public class XamlXLoadMethodDelegateNode : XamlXValueWithSideEffectNodeBase,
        IXamlXAstEmitableNode
    {
        public IXamlXType DelegateType { get; }
        public IXamlXMethod Method { get; }

        public XamlXLoadMethodDelegateNode(IXamlXLineInfo lineInfo, IXamlXAstValueNode value,
            IXamlXType delegateType, IXamlXMethod method) : base(lineInfo, value)
        {
            DelegateType = delegateType;
            Method = method;
            Type = new XamlXAstClrTypeReference(value, DelegateType);
        }

        public override IXamlXAstTypeReference Type { get; }
        public XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            context.Emit(Value, codeGen, Method.DeclaringType);
            codeGen
                .Ldftn(Method)
                .Newobj(DelegateType.Constructors.FirstOrDefault(ct =>
                    ct.Parameters.Count == 2 && ct.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));
            return XamlXNodeEmitResult.Type(0, DelegateType);
        }
    }
}