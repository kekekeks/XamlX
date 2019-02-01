using System;
using System.Collections.Generic;
using XamlIl;
using Xunit;

namespace XamlParserTests
{
    public class IntrinsicsTestsClass
    {
        public object ObjectProperty { get; set; }
        public int IntProperty { get; set; }
        public Type TypeProperty { get; set; }
    }
    
    public class IntrinsicsTests : CompilerTestBase
    {
        [Fact]
        public void Null_Extension_Should_Be_Operational()
        {
            var res = (IntrinsicsTestsClass)CompileAndRun(@"
<IntrinsicsTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <IntrinsicsTestsClass.ObjectProperty><x:Null/></IntrinsicsTestsClass.ObjectProperty>
</IntrinsicsTestsClass>");
            Assert.Null(res.ObjectProperty);
        }

        [Fact]
        public void Null_Extension_Should_Cause_Compilation_Error_When_Applied_To_Value_Type()
        {
            Assert.Throws<XamlIlLoadException>(() => Compile(@"
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
            var res = (IntrinsicsTestsClass)CompileAndRun($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic'
>
    <IntrinsicsTestsClass.TypeProperty>{typeExt}</IntrinsicsTestsClass.TypeProperty>
</IntrinsicsTestsClass>");
            Assert.Equal(expectedType, res.TypeProperty);
        }
    }
}