using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.Transform;

namespace XamlIl.TypeSystem
{
#if !XAMLIL_INTERNAL
    public
#endif
    class TypeSystemHelpers
    {
        public static int ConvertLiteralToInt(object literal)
        {
            if (literal is uint ui)
                return unchecked((int) ui);
            return (int) Convert.ChangeType(literal, typeof(int));
        }
        
        public static long ConvertLiteralToLong(object literal)
        {
            if (literal is ulong ui)
                return unchecked((long) ui);
            return (long) Convert.ChangeType(literal, typeof(long));
        }

        public static void EmitFieldLiteral(IXamlIlField field, IXamlIlEmitter codeGen)
        {
            var ftype = field.FieldType.IsEnum ? field.FieldType.GetEnumUnderlyingType() : field.FieldType;
                    
            if (ftype.Name == "UInt64" || ftype.Name == "Int64")
                codeGen.Emit(OpCodes.Ldc_I8,
                    TypeSystemHelpers.ConvertLiteralToLong(field.GetLiteralValue()));
            else if (ftype.Name == "Double")
                codeGen.Emit(OpCodes.Ldc_R8, (double) field.GetLiteralValue());
            else if (ftype.Name == "Single")
                codeGen.Emit(OpCodes.Ldc_R4, (float) field.GetLiteralValue());
            else if (ftype.Name == "String")
                codeGen.Emit(OpCodes.Ldstr, (string) field.GetLiteralValue());
            else
                codeGen.Emit(OpCodes.Ldc_I4,
                    TypeSystemHelpers.ConvertLiteralToInt(field.GetLiteralValue()));
        }

        public static XamlIlConstantNode GetLiteralFieldConstantNode(IXamlIlField field, IXamlIlLineInfo info)
            => new XamlIlConstantNode(info, field.FieldType, GetLiteralFieldConstantValue(field));
        
        public static object GetLiteralFieldConstantValue(IXamlIlField field)
        {
            var value = field.GetLiteralValue();
            
            //This code is needed for SRE backend that returns an actual enum instead of just int
            if (value.GetType().IsEnum) 
                value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());

            return value;
        }



        public static bool TryGetEnumValueNode(IXamlIlType enumType, string value, IXamlIlLineInfo lineInfo, out XamlIlConstantNode rv)
        {
            if (TryGetEnumValue(enumType, value, out var constant))
            {
                rv = new XamlIlConstantNode(lineInfo, enumType, constant);
                return true;
            }

            rv = null;
            return false;
        }
        
        public static bool TryGetEnumValue(IXamlIlType enumType, string value, out object rv)
        {
            rv = null;
            if (long.TryParse(value, out var parsedLong))
            {
                var enumTypeName = enumType.GetEnumUnderlyingType().Name;
                rv = enumTypeName == "Int32" || enumTypeName == "UInt32" ?
                    unchecked((int)parsedLong) :
                    (object)parsedLong;
                return true;
            }
            
            var values = enumType.CustomAttributes.Any(a => a.Type.Name == "FlagsAttribute") ?
                value.Split(',').Select(x => x.Trim()).ToArray() :
                new[] { value };
            object cv = null;
            for (var c = 0; c < values.Length; c++)
            {
                var enumValueField = enumType.Fields.FirstOrDefault(f => f.Name == values[c]);
                if (enumValueField == null)
                    return false;
                var enumValue = GetLiteralFieldConstantValue(enumValueField);
                if (c == 0)
                    cv = enumValue;
                else
                    cv = Or(cv, enumValue);
            }

            rv = cv;
            return true;
        }

        static object Or(object l, object r)
        {
            if (l is byte lb)
                return lb | (byte)r;
            if (l is sbyte lsb)
                return lsb | (sbyte)r;
            if (l is ushort lus)
                return lus | (ushort)r;
            if (l is short ls)
                return ls | (short)r;
            if (l is uint lui)
                return lui | (uint)r;
            if (l is int li)
                return li | (int)r;
            if (l is ulong lul)
                return lul | (ulong)r;
            if (l is long ll)
                return ll | (long)r;
            throw new ArgumentException("Unsupported type " + l.GetType());
        }

        public static bool ParseConstantIfTypeAllows(string s, IXamlIlType type, IXamlIlLineInfo info, out XamlIlConstantNode rv)
        {
            rv = null;
            if (type.Namespace != "System")
                return false;

            object Parse()
            {
                if (type.Name == "Byte")
                    return byte.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "SByte")
                    return sbyte.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "Int16")
                    return Int16.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "UInt16")
                    return UInt16.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "Int32")
                    return Int32.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "UInt32")
                    return UInt32.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "Int64")
                    return Int64.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "UInt64")
                    return UInt64.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "Single")
                    return Single.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "Double")
                    return Double.Parse(s, CultureInfo.InvariantCulture);
                if (type.Name == "Boolean")
                    return Boolean.Parse(s);
                return null;
            }

            var r = Parse();
            if (r != null)
            {
                rv = new XamlIlConstantNode(info, type, r);
                return true;
            }

            return false;
        }

        public static void EmitConvert(XamlIlEmitContext context, IXamlIlEmitter ilgen, IXamlIlLineInfo node, IXamlIlType what,
            IXamlIlType to, IXamlIlLocal local)
        {
            EmitConvert(context, node, what, to, lda => ilgen.Emit(lda ? OpCodes.Ldloca : OpCodes.Ldloc, local));
        }

        public static void EmitConvert(XamlIlEmitContext context, IXamlIlEmitter ilgen, IXamlIlLineInfo node,
            IXamlIlType what,
            IXamlIlType to)
        {
            XamlIlLocalsPool.PooledLocal local = null;

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
        
        public static void EmitConvert(XamlIlEmitContext context, IXamlIlLineInfo node, IXamlIlType what,
            IXamlIlType to, Func<bool, IXamlIlEmitter> ld)
        {
            if (what.Equals(to))
                ld(false);
            else if (what == XamlIlPseudoType.Null)
            {
                
                if (to.IsValueType)
                {
                    if (to.GenericTypeDefinition?.Equals(context.Configuration.WellKnownTypes.NullableT) == true)
                    {
                        using (var loc = context.GetLocal(to))
                            ld(false)
                                .Pop()
                                .Ldloca(loc.Local)
                                .Emit(OpCodes.Initobj, to)
                                .Ldloc(loc.Local);

                    }
                    else
                        throw new XamlIlLoadException("Unable to convert {x:Null} to " + to.GetFqn(), node);
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
                    throw new XamlIlLoadException(
                        $"Don't know how to convert value type {what.GetFullName()} to value type {to.GetFullName()}",
                        node);
            }
            else if (!to.IsValueType && what.IsValueType)
            {
                if (!to.IsAssignableFrom(what))
                    throw new XamlIlLoadException(
                        $"Don't know how to convert value type {what.GetFullName()} to reference type {to.GetFullName()}",
                        node);
                ld(false).Box(what);
            }
            else if(to.IsValueType && !what.IsValueType)
            {
                if (!(what.Namespace == "System" && what.Name == "Object"))
                    throw new XamlIlLoadException(
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
                    throw new XamlIlLoadException(
                        $"Don't know how to convert reference type {what.GetFullName()} to reference type {to.GetFullName()}",
                        node);
            }
        }
    }
}
