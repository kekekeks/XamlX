using System;
using System.ComponentModel;
using System.Globalization;

namespace XamlParserTests
{
    public class TestConverter : TypeConverter
    {
        public event Action<CultureInfo, ITypeDescriptorContext> ConvertFromEventRequiredAssert;

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            ConvertFromEventRequiredAssert?.Invoke(culture, context);
            return new ConvertersTestsClassWithConverter { Value = (string)value };
        }
    }
}
