using System;
using System.ComponentModel;
using System.Globalization;
using Xunit;

namespace XamlParserTests
{
    public class ConvertersTestClass
    {
        [Content]
        public object ContentProperty { get; set; }
        public long Int64Property { get; set; }
        public bool BoolProperty { get; set; }
        public double DoubleProperty { get; set; }
        public float FloatProperty { get; set; }
        public TimeSpan TimeSpanProperty { get; set; }
        public ConvertersTestValueType CustomProperty { get; set; }
        public ConvertersTestsClassWithConverter ConverterProperty { get; set; }
    }

    public struct ConvertersTestValueType
    {
        public string Value { get; set; }
        public override string ToString() => Value;

        public static ConvertersTestValueType Parse(string s, IFormatProvider prov)
        {
            Assert.NotNull(prov);
            return new ConvertersTestValueType() {Value = s};
        }
    }

    [TypeConverter(typeof(ConvertersTests.TestConverter))]
    public class ConvertersTestsClassWithConverter
    {
        public string Value { get; set; }
        public override string ToString() => Value;
    }
    
    
    
    public class ConvertersTests : CompilerTestBase
    {

        public class TestConverter : TypeConverter
        {
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                Assert.Equal(CultureInfo.InvariantCulture, culture);
                Assert.NotNull(context.GetService<ITestRootObjectProvider>().RootObject);
                return new ConvertersTestsClassWithConverter {Value = (string) value};
            }
        }
        
        [Theory,
            InlineData("Int64Property", "1"),
            InlineData("BoolProperty", "True"),
            InlineData("DoubleProperty", "1.5"),
            InlineData("FloatProperty", "2.5"),
            InlineData("CustomProperty", "Custom"),
            InlineData("TimeSpanProperty", "01:10:00"),
            InlineData("ConverterProperty", "CustomConverter"),
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
        
        [Theory,
         InlineData("x:Int32", "1"),
         InlineData("x:Double", "1.5"),
         InlineData("x:Single", "2.5"),
         InlineData("x:String", "Some string"),
         InlineData("x:TimeSpan", "01:10:00"),
         InlineData("sys:Int32", "1"),
         InlineData("sys:Double", "1.5"),
         InlineData("sys:Single", "2.5"),
         InlineData("sys:String", "Some string"),
         InlineData("sys:TimeSpan", "01:10:00")
        ]
        public void Primitive_Types_Are_Properly_Parsed(string type, string value)
        {
            var res = (ConvertersTestClass) CompileAndRun($@"
<ConvertersTestClass
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
    <{type}>{value}</{type}>
</ConvertersTestClass>");
            CultureInfo old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                var v = res.ContentProperty;
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