using System;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace XamlIl.Ast
{
    public class XamlIlNullExtensionNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlNullExtensionNode(IXamlIlLineInfo lineInfo) : base(lineInfo)
        {
            Type = new XamlIlAstClrTypeReference(lineInfo, XamlIlPseudoType.Null);
        }

        public IXamlIlAstTypeReference Type { get; }
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            codeGen.Generator.Emit(OpCodes.Ldnull);
            return XamlIlNodeEmitResult.Type(XamlIlPseudoType.Null);
        }
    }

    public class XamlIlTypeExtensionNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        private readonly IXamlIlType _systemType;

        public XamlIlTypeExtensionNode(IXamlIlLineInfo lineInfo, IXamlIlAstTypeReference value,
            IXamlIlType systemType) : base(lineInfo)
        {
            _systemType = systemType;
            Type = new XamlIlAstClrTypeReference(this, systemType);
            Value = value;
        }

        public IXamlIlAstTypeReference Type { get; }
        public IXamlIlAstTypeReference Value { get; set; }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            Value = Value.Visit(visitor) as IXamlIlAstTypeReference;
        }

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            var type = Value.GetClrType();
            var method = _systemType.Methods.FirstOrDefault(m =>
                m.Name == "GetTypeFromHandle" && m.Parameters.Count == 1 &&
                m.Parameters[0].Name == "RuntimeTypeHandle");

            if (method == null)
                throw new XamlIlTypeSystemException(
                    $"Unable to find GetTypeFromHandle(RuntimeTypeHandle) on {_systemType.GetFqn()}");
            codeGen.Generator
                .Emit(OpCodes.Ldtoken, type)
                .Emit(OpCodes.Call, method);
            return XamlIlNodeEmitResult.Type(_systemType);
        }
    }

    public class XamlIlStaticExtensionNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public XamlIlStaticExtensionNode(XamlIlAstNewInstanceNode lineInfo, IXamlIlAstTypeReference targetType, string member) : base(lineInfo)
        {
            TargetType = targetType;
            Member = member;
        }

        public string Member { get; set; }
        public IXamlIlAstTypeReference TargetType { get; set; }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            TargetType = (IXamlIlAstTypeReference) TargetType.Visit(visitor);
        }

        IXamlIlMember ResolveMember(IXamlIlType type)
        {
            return type.Fields.FirstOrDefault(f => f.IsPublic && f.IsStatic && f.Name == Member) ??
                   (IXamlIlMember) type.Properties.FirstOrDefault(p =>
                       p.Name == Member && p.Getter != null && p.Getter.IsPublic && p.Getter.IsStatic);
        }
        
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            var type = TargetType.GetClrType();
            var member = ResolveMember(type);
            if (member is IXamlIlProperty prop)
            {
                codeGen.Generator.Emit(OpCodes.Call, prop.Getter);
                return XamlIlNodeEmitResult.Type(prop.Getter.ReturnType);
            }
            else if (member is IXamlIlField field)
            {
                if (field.IsLiteral)
                {
                    var ftype = field.FieldType.IsEnum ? field.FieldType.GetEnumUnderlyingType() : field.FieldType;
                    
                    if (ftype.Name == "UInt64" || ftype.Name == "Int64")
                        codeGen.Generator.Emit(OpCodes.Ldc_I8,
                            TypeSystemHelpers.ConvertLiteralToLong(field.GetLiteralValue()));
                    else if (ftype.Name == "Double")
                        codeGen.Generator.Emit(OpCodes.Ldc_R8, (double) field.GetLiteralValue());
                    else if (ftype.Name == "Single")
                        codeGen.Generator.Emit(OpCodes.Ldc_R4, (float) field.GetLiteralValue());
                    else if (ftype.Name == "String")
                        codeGen.Generator.Emit(OpCodes.Ldstr, (string) field.GetLiteralValue());
                    else
                        codeGen.Generator.Emit(OpCodes.Ldc_I4,
                            TypeSystemHelpers.ConvertLiteralToInt(field.GetLiteralValue()));
                }
                else
                    codeGen.Generator.Emit(OpCodes.Ldsfld, field);
                return XamlIlNodeEmitResult.Type(field.FieldType);
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
            return XamlIlPseudoType.Unknown;
        }
        
        public IXamlIlAstTypeReference Type => new XamlIlAstClrTypeReference(this, ResolveReturnType());
    }

    public class XamlIlConstantNode : XamlIlAstNode, IXamlIlAstValueNode, IXamlIlAstEmitableNode
    {
        public object Constant { get; }

        public XamlIlConstantNode(IXamlIlLineInfo lineInfo, IXamlIlType type, object constant) : base(lineInfo)
        {
            if (!constant.GetType().IsPrimitive)
                throw new ArgumentException($"Don't know how to emit {constant.GetType()} constant");
            Constant = constant;
            Type = new XamlIlAstClrTypeReference(lineInfo, type);

        }

        public IXamlIlAstTypeReference Type { get; }
        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (Constant is string)
                codeGen.Generator.Emit(OpCodes.Ldstr, (string) Constant);
            else if (Constant is long || Constant is ulong)
                codeGen.Generator.Emit(OpCodes.Ldc_I8, TypeSystemHelpers.ConvertLiteralToLong(Constant));
            else if (Constant is float f)
                codeGen.Generator.Emit(OpCodes.Ldc_R4, f);
            else if (Constant is double d)
                codeGen.Generator.Emit(OpCodes.Ldc_R8, d);
            else
                codeGen.Generator.Emit(OpCodes.Ldc_I4, TypeSystemHelpers.ConvertLiteralToInt(Constant));
            return XamlIlNodeEmitResult.Type(Type.GetClrType());
        }
    }
}