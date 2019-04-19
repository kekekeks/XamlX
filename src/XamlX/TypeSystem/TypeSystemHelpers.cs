using System;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform;

namespace XamlX.TypeSystem
{
    public class TypeSystemHelpers
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

        public static void EmitFieldLiteral(IXamlField field, IXamlILEmitter codeGen)
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

        public static XamlConstantNode GetLiteralFieldConstantNode(IXamlField field, IXamlLineInfo info)
        {
            var value = field.GetLiteralValue();
            
            //This code is needed for SRE backend that returns an actual enum instead of just int
            if (value.GetType().IsEnum) 
                value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());
            
            return new XamlConstantNode(info, field.FieldType, value);
        }

        public static bool ParseConstantIfTypeAllows(string s, IXamlType type, IXamlLineInfo info, out XamlConstantNode rv)
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
                rv = new XamlConstantNode(info, type, r);
                return true;
            }

            return false;
        }
        
        public static void EmitConvert(XamlEmitContext context, IXamlLineInfo node, IXamlType what,
            IXamlType to, Func<bool, IXamlILEmitter> ld)
        {
            if (what.Equals(to))
                ld(false);
            else if (what == XamlPseudoType.Null)
            {
                
                if (to.IsValueType)
                {
                    if (to.GenericTypeDefinition.Equals(context.Configuration.WellKnownTypes.NullableT))
                    {
                        using (var loc = context.GetLocal(to))
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
