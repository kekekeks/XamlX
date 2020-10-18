using System.Globalization;
using Xunit;

namespace XamlParserTests
{
    public class ConvertersTests : CompilerTestBase
    {
        [Theory,
         InlineData("Int64Property", "1"),
         InlineData("BoolProperty", "True"),
         InlineData("DoubleProperty", "1.5"),
         InlineData("FloatProperty", "2.5"),
         InlineData("CustomProperty", "Custom"),
         InlineData("TimeSpanProperty", "01:10:00"),
         InlineData("TypeWithConverterProperty", "CustomConverter"),
         InlineData("PropertyWithConverter", "CustomConverterProperty"),
         InlineData("UriKindProperty", "Relative"),
         InlineData("UriKindProperty", "150"),
         InlineData("EnumProperty", "Second"),
         InlineData("EnumProperty", "First, Third"),
         InlineData("EnumProperty", "100500"),
        ]
        public void Converters_Are_Operational(string property, string value)
            => CheckConversion(property, value, value);

        [Fact]
        public void Type_Properties_Are_Converted()
        {
            CheckConversion("TypeProperty", "ConvertersTestClass", 
                typeof(ConvertersTestClass).ToString());
        }

        private void CheckConversion(string property, string value, string expected)
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
                Assert.Equal(expected, ts);
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

        [Fact]
        public void Constructor_Parameters_Should_Be_Converted()
        {
            var res = (ConvertersTestsClassWithConstructor) CompileAndRun($@"
<ConvertersTestsClassWithConstructor
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
    <x:Arguments>
        <sys:String>123</sys:String>
        <sys:String>01:10:00</sys:String>
    </x:Arguments>
</ConvertersTestsClassWithConstructor>");
            Assert.Equal(123, res.Int);
            Assert.Equal("01:10:00", res.Converted.ToString());
            
        }
    }
}