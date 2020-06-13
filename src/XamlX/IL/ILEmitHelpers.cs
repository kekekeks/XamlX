using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    class ILEmitHelpers
    {
        public static void EmitFieldLiteral(IXamlField field, IXamlILEmitter codeGen)
        {
            var ftype = field.FieldType.IsEnum ? field.FieldType.GetEnumUnderlyingType() : field.FieldType;

            if (ftype.Name == "UInt64" || ftype.Name == "Int64")
                codeGen.Emit(OpCodes.Ldc_I8,
                    TypeSystemHelpers.ConvertLiteralToLong(field.GetLiteralValue()));
            else if (ftype.Name == "Double")
                codeGen.Emit(OpCodes.Ldc_R8, (double)field.GetLiteralValue());
            else if (ftype.Name == "Single")
                codeGen.Emit(OpCodes.Ldc_R4, (float)field.GetLiteralValue());
            else if (ftype.Name == "String")
                codeGen.Emit(OpCodes.Ldstr, (string)field.GetLiteralValue());
            else
                codeGen.Emit(OpCodes.Ldc_I4,
                    TypeSystemHelpers.ConvertLiteralToInt(field.GetLiteralValue()));
        }

        public static void EmitConvert(XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter ilgen, IXamlLineInfo node, IXamlType what,
            IXamlType to, IXamlLocal local)
        {
            EmitConvert(context, node, what, to, lda => ilgen.Emit(lda ? OpCodes.Ldloca : OpCodes.Ldloc, local));
        }

        public static void EmitConvert(XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter ilgen, IXamlLineInfo node,
            IXamlType what,
            IXamlType to)
        {
            XamlLocalsPool.PooledLocal local = null;

            EmitConvert(context, node, what, to, lda =>
            {
                if (!lda)
                    return ilgen;
                local = ilgen.LocalsPool.GetLocal(what);
                ilgen
                    .Stloc(local.Local)
                    .Ldloca(local.Local);
                return ilgen;
            });
            local?.Dispose();
        }
        
        public static void EmitConvert(XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlLineInfo node, IXamlType what,
            IXamlType to, Func<bool, IXamlILEmitter> ld)
        {
            if (what.Equals(to))
                ld(false);
            else if (what == XamlPseudoType.Null)
            {
                
                if (to.IsValueType)
                {
                    if (to.GenericTypeDefinition?.Equals(context.Configuration.WellKnownTypes.NullableT) == true)
                    {
                        using (var loc = context.GetLocalOfType(to))
                            ld(false)
                                .Pop()
                                .Ldloca(loc.Local)
                                .Emit(OpCodes.Initobj, to)
                                .Ldloc(loc.Local);

                    }
                    else
                        throw new XamlLoadException("Unable to convert {x:Null} to " + to.GetFqn(), node);
                }
                else
                    ld(false);
            }
            else if (what.IsValueType && to.IsValueType)
            {
                if (to.IsNullableOf(what))
                {
                    ld(false).Emit(OpCodes.Newobj,
                        to.Constructors.First(c =>
                            c.Parameters.Count == 1 && c.Parameters[0].Equals(what)));
                }
                else if (what.IsNullableOf(what))
                    ld(true)
                        .EmitCall(what.FindMethod(m => m.Name == "get_Value"));
                else
                    throw new XamlLoadException(
                        $"Don't know how to convert value type {what.GetFullName()} to value type {to.GetFullName()}",
                        node);
            }
            else if (!to.IsValueType && what.IsValueType)
            {
                if (!to.IsAssignableFrom(what))
                    throw new XamlLoadException(
                        $"Don't know how to convert value type {what.GetFullName()} to reference type {to.GetFullName()}",
                        node);
                ld(false).Box(what);
            }
            else if(to.IsValueType && !what.IsValueType)
            {
                if (!(what.Namespace == "System" && what.Name == "Object"))
                    throw new XamlLoadException(
                        $"Don't know how to convert reference type {what.GetFullName()} to value type {to.GetFullName()}",
                        node);
                ld(false).Unbox_Any(to);
            }
            else
            {
                if (to.IsAssignableFrom(what))
                    // Downcast, always safe
                    ld(false);
                else if (what.IsInterface || what.IsAssignableFrom(to))
                    // Upcast or cast from interface, might throw InvalidCastException
                    ld(false).Emit(OpCodes.Castclass, to);
                else
                    // Types are completely unrelated, e. g. string to List<int> conversion attempt
                    throw new XamlLoadException(
                        $"Don't know how to convert reference type {what.GetFullName()} to reference type {to.GetFullName()}",
                        node);
            }
        }
    }
}
