using System;
using XamlX;
using Xunit;

namespace XamlParserTests
{
    public class MarkupExtensionTests : CompilerTestBase
    {       
        [Fact]
        public void Object_Should_Be_Casted_To_String()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' StringProperty='{ObjectTestExtension Returned=test}'/>");
            Assert.Equal("test", res.StringProperty);
        }
        
        private IServiceProvider CreateValueProvider(object value)=>new DictionaryServiceProvider
        {
            [typeof(ExtensionValueHolder)] = new ExtensionValueHolder() {Value = value}
        };
        
        [Fact]
        public void Object_Should_Be_Casted_To_Value_Type()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' 
    IntProperty='{ServiceProviderValue}'/>", CreateValueProvider(123));
            Assert.Equal(123, res.IntProperty);
        }
        
        [Fact]
        public void Object_Should_Be_Casted_To_Nullable_Value_Type()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' 
    NullableIntProperty='{ServiceProviderValue}'/>", CreateValueProvider(123));
            Assert.Equal(123, res.NullableIntProperty);
        }

        [Fact]
        public void Extensions_Should_Be_Able_To_Populate_Content_Lists()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test'>
    <ServiceProviderValue/>
    <ServiceProviderValue/> 
</MarkupExtensionTestsClass>", CreateValueProvider(123));
            Assert.Equal(new[] {123, 123}, res.IntList);
        }
        
        [Fact]
        public void Extensions_Should_Be_Able_To_Populate_Lists()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' IntList2='{ServiceProviderValue}'>
</MarkupExtensionTestsClass>", CreateValueProvider(123));
            Assert.Equal(new[] {123}, res.IntList2);
        }

        [Fact]
        public void Extensions_Should_Be_Able_To_Populate_Content_Dictionaries()
        {
            var res = (MarkupExtensionContentDictionaryClass) CompileAndRun(@"
<MarkupExtensionContentDictionaryClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ServiceProviderValue x:Key='First'/>
    <ServiceProviderValue x:Key='Second'/> 
</MarkupExtensionContentDictionaryClass>", CreateValueProvider(123));
            Assert.Equal(2, res.IntDic.Count);
            Assert.Equal(123, res.IntDic["First"]);
            Assert.Equal(123, res.IntDic["Second"]);
        }

        [Fact]
        public void Non_Boxed_Value_Type_Should_Be_Convertable_To_Nullable()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' 
    NullableIntProperty='{ServiceProviderIntValue}'/>", CreateValueProvider(123));
            Assert.Equal(123, res.NullableIntProperty);
        }
        
        [Fact]
        public void Non_Boxed_Value_Type_Should_Be_Convertable_To_Object()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' 
    ObjectProperty='{ServiceProviderIntValue}'/>", CreateValueProvider(123));
            Assert.Equal(123, res.ObjectProperty);
        }

        [Fact]
        public void Unknown_Reference_Type_To_Value_Type_Should_Trigger_InvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() =>
                (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' 
    IntProperty='{ServiceProviderValue}'/>", CreateValueProvider("test")));
        }
        
        [Fact]
        public void Value_Type_To_Reference_Type_Should_Trigger_Compile_Error()
        {
            Assert.Throws<XamlLoadException>(() => Compile(@"
<MarkupExtensionTestsClass xmlns='test' 
    StringProperty='{ServiceProviderIntValue}'/>"));
        }
        
        [Fact]
        public void Mismatched_Value_Type_To_Value_Type_Should_Trigger_Compile_Error()
        {
            Assert.Throws<XamlLoadException>(() => Compile(@"
<MarkupExtensionTestsClass xmlns='test' 
    DoubleProperty='{ServiceProviderIntValue}'/>"));
        }
        
        [Fact]
        public void Mismatched_Reference_Type_To_Reference_Type_Should_Trigger_InvalidCastException()
        {
            var val = new Uri("http://test/");
            Assert.Throws<InvalidCastException>(() =>
                (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' 
    StringProperty='{ServiceProviderValue}'/>", CreateValueProvider(val)));
        }
    }
}
