using System;
using System.ComponentModel;
using System.Globalization;

namespace XamlParserTests
{
    public class PropertyTestConverter : TypeConverter
    {
        public static event Action<CultureInfo, ITypeDescriptorContext> ConvertFromEventRequiredAssert;

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            ConvertFromEventRequiredAssert.Invoke(culture, context);
            return new ConvertersTestsClassWithoutConverter { Value = (string)value };
        }
    }
}
