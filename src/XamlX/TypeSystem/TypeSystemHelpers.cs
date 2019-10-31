using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform;

namespace XamlX.TypeSystem
{
#if !XAMLX_INTERNAL
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

        public static XamlConstantNode GetLiteralFieldConstantNode(IXamlField field, IXamlLineInfo info)
            => new XamlConstantNode(info, field.FieldType, GetLiteralFieldConstantValue(field));
        
        public static object GetLiteralFieldConstantValue(IXamlField field)
        {
            var value = field.GetLiteralValue();
            
            //This code is needed for SRE backend that returns an actual enum instead of just int
            if (value.GetType().IsEnum) 
                value = Convert.ChangeType(value, value.GetType().GetEnumUnderlyingType());

            return value;
        }



        public static bool TryGetEnumValueNode(IXamlType enumType, string value, IXamlLineInfo lineInfo, out XamlConstantNode rv)
        {
            if (TryGetEnumValue(enumType, value, out var constant))
            {
                rv = new XamlConstantNode(lineInfo, enumType, constant);
                return true;
            }

            rv = null;
            return false;
        }
        
        public static bool TryGetEnumValue(IXamlType enumType, string value, out object rv)
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
    }
}
