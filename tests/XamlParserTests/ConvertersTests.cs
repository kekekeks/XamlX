using System;
using System.Globalization;
using Xunit;

namespace XamlParserTests
{
    public class ConvertersTestClass
    {
        public long Int64Property { get; set; }
        public double DoubleProperty { get; set; }
        public float FloatProperty { get; set; }
        public TimeSpan TimeSpanProperty { get; set; }
        public ConvertersTestValueType CustomProperty { get; set; }
    }

    public struct ConvertersTestValueType
    {
        public string Value { get; set; }
        public override string ToString() => Value;

        public static ConvertersTestValueType  Parse(string s, IFormatProvider prov)
        {
            Assert.NotNull(prov);
            return new ConvertersTestValueType() {Value = s};
        }
    }
    
    public class ConvertersTests : CompilerTestBase
    {
        [Theory,
            InlineData("Int64Property", "1"),
            InlineData("DoubleProperty", "1.5"),
            InlineData("FloatProperty", "2.5"),
            InlineData("CustomProperty", "Custom"),
            InlineData("TimeSpanProperty", "01:10:00")
        ]
        public void Parse_Converters_Are_Operational(string property, string value)
        {
            var res = (ConvertersTestClass) CompileAndRun($@"
<ConvertersTestClass
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic' {property}='{value}'
/>");
            CultureInfo old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                var v = res.GetType().GetProperty(property).GetValue(res);
                var ts = v.ToString();
                Assert.Equal(value, ts);
            }
            finally
            {
                CultureInfo.CurrentCulture = old;
            }
        }
    }
}