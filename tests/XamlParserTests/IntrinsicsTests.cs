using System;
using System.Collections.Generic;
using XamlX;
using Xunit;

namespace XamlParserTests
{
    public class IntrinsicsTestsClass
    {
        public object ObjectProperty { get; set; }
        public int IntProperty { get; set; }
        public Type TypeProperty { get; set; }

        public static object StaticProp { get; } = "StaticPropValue";
        public static object StaticField = "StaticFieldValue";
        public const string StringConstant = "ConstantValue";
        public const int IntConstant = 100;
        public const float FloatConstant = 2;
        public const double DoubleConstant = 3;
        
    }

    public enum IntrinsicsTestsEnum : long
    {
        Foo = 100500
    }

    public class IntrinsicsTests : CompilerTestBase
    {
        [Fact]
        public void Null_Extension_Should_Be_Operational()
        {
            var res = (IntrinsicsTestsClass) CompileAndRun(@"
<IntrinsicsTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <IntrinsicsTestsClass.ObjectProperty><x:Null/></IntrinsicsTestsClass.ObjectProperty>
</IntrinsicsTestsClass>");
            Assert.Null(res.ObjectProperty);
        }

        [Fact]
        public void Null_Extension_Should_Cause_Compilation_Error_When_Applied_To_Value_Type()
        {
            Assert.Throws<XamlLoadException>(() => Compile(@"
<IntrinsicsTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <IntrinsicsTestsClass.IntProperty><x:Null/></IntrinsicsTestsClass.IntProperty>
</IntrinsicsTestsClass>"));
        }

        [Theory,
         InlineData(typeof(IntrinsicsTestsClass), "<x:Type TypeName='IntrinsicsTestsClass' />"),
         InlineData(typeof(List<string>), "<x:Type x:TypeArguments='x:String' TypeName='scg:List' />")
        ]
        public void Type_Extension_Resolves_Types(Type expectedType, string typeExt)
        {
            var res = (IntrinsicsTestsClass) CompileAndRun($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
>
    <IntrinsicsTestsClass.TypeProperty>{typeExt}</IntrinsicsTestsClass.TypeProperty>
</IntrinsicsTestsClass>");
            Assert.Equal(expectedType, res.TypeProperty);
        }

        [Theory,
         InlineData("StaticPropValue", "IntrinsicsTestsClass.StaticProp"),
         InlineData("StaticFieldValue", "IntrinsicsTestsClass.StaticField"),
         InlineData("ConstantValue", "IntrinsicsTestsClass.StringConstant"),
         InlineData(100, "IntrinsicsTestsClass.IntConstant"),
         InlineData(2f, "IntrinsicsTestsClass.FloatConstant"),
         InlineData(3d, "IntrinsicsTestsClass.DoubleConstant"),
        ]
        public void Static_Extension_Resolves_Values(object expected, string r)
        {
            var res = (IntrinsicsTestsClass) CompileAndRun($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic'
>
    <IntrinsicsTestsClass.ObjectProperty><x:Static Member='{r}'/></IntrinsicsTestsClass.ObjectProperty>
</IntrinsicsTestsClass>");
            Assert.Equal(expected, res.ObjectProperty);
        }

        [Fact]
        public void Static_Extension_Resolves_Enum_Values()
        {
            Static_Extension_Resolves_Values(IntrinsicsTestsEnum.Foo, "IntrinsicsTestsEnum.Foo");
        }
    }
}