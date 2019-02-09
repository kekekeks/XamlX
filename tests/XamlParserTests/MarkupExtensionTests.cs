using System;
using System.Collections.Generic;
using XamlIl;
using XamlIl.TypeSystem;
using Xunit;

namespace XamlParserTests
{
    public class MarkupExtensionTestsClass
    {
        public int IntProperty { get; set; }
        public double DoubleProperty { get; set; }
        public int? NullableIntProperty { get; set; }
        public string StringProperty { get; set; }
        public object ObjectProperty { get; set; }
        [Content]
        public List<int> IntList { get; } = new List<int>();
        public List<int> IntList2 { get; } = new List<int>();
    }

    public class ObjectTestExtension
    {
        public object Returned { get; set; }
        public object ProvideValue()
        {
            return Returned;
        }
    }

    class DictionaryServiceProvider : Dictionary<Type, object>, IServiceProvider
    {
        
        public object GetService(Type serviceType)
        {
            TryGetValue(serviceType, out var impl);
            return impl;
        }
    }

    public class ExtensionValueHolder
    {
        public object Value { get; set; }
    }

    public class ServiceProviderValueExtension
    {
        public object ProvideValue(IServiceProvider sp) =>
            ((ExtensionValueHolder) sp.GetService(typeof(ExtensionValueHolder))).Value;
    }

    public class ServiceProviderIntValueExtension
    {
        public int ProvideValue(IServiceProvider sp) =>
            (int) ((ExtensionValueHolder) sp.GetService(typeof(ExtensionValueHolder))).Value;
    }

    public class CustomConvertedType
    {
        public string Value { get; set; }
    }
    
    

    delegate void ApplyNonMatchingMarkupExtensionDelegate(object target, object property, IServiceProvider prov,
        object value);
    
    public class MarkupExtensionTests : CompilerTestBase
    {
        public MarkupExtensionTests()
        {
            Configuration.TypeMappings.MarkupExtensionCustomResultHandler =
                Configuration.TypeSystem.FindType("XamlParserTests.MarkupExtensionTests")
                    .FindMethod(m => m.Name == "ApplyNonMatchingMarkupExtension");
        }
        
        [Fact]
        public void Object_Should_Be_Casted_To_String()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test'>
    <MarkupExtensionTestsClass.StringProperty>
        <ObjectTestExtension Returned='test'/>
    </MarkupExtensionTestsClass.StringProperty>
</MarkupExtensionTestsClass>");
            Assert.Equal("test", res.StringProperty);
        }
        
        IServiceProvider CreateValueProvider(object value, ApplyNonMatchingMarkupExtensionDelegate convert = null)=>new DictionaryServiceProvider
        {
            [typeof(ExtensionValueHolder)] = new ExtensionValueHolder() {Value = value},
            [typeof(ApplyNonMatchingMarkupExtensionDelegate)] = convert
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
<MarkupExtensionTestsClass xmlns='test'>
    <MarkupExtensionTestsClass.IntList2>
        <ServiceProviderValue/>
        <ServiceProviderValue/>
    </MarkupExtensionTestsClass.IntList2> 
</MarkupExtensionTestsClass>", CreateValueProvider(123));
            Assert.Equal(new[] {123, 123}, res.IntList2);
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


        public static void ApplyNonMatchingMarkupExtension(object target, object property, IServiceProvider prov,
            object value)
        {
            ((ApplyNonMatchingMarkupExtensionDelegate)
                    prov.GetService(typeof(ApplyNonMatchingMarkupExtensionDelegate)))
                    (target, property, prov, value);
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
            Assert.Throws<XamlIlLoadException>(() => Compile(@"
<MarkupExtensionTestsClass xmlns='test' 
    StringProperty='{ServiceProviderIntValue}'/>"));
        }
        
        [Fact]
        public void Mismatched_Value_Type_To_Value_Type_Should_Trigger_Compile_Error()
        {
            Assert.Throws<XamlIlLoadException>(() => Compile(@"
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

        [Fact]
        public void Custom_Converted_Types_Should_Be_Passed_To_Apply_Callback()
        {
            Configuration.TypeMappings.MarkupExtensionCustomResultTypes.Add(
                Configuration.TypeSystem.GetType(typeof(CustomConvertedType).FullName));
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' 
    ObjectProperty='{ServiceProviderValue}'/>", CreateValueProvider(new CustomConvertedType()
            {
                Value = "test"
            }, (t, p, s, v) =>
            {
                if (v is CustomConvertedType ct)
                    t.GetType().GetProperty((string) p).SetValue(t, ct.Value);
            }));
            Assert.Equal("test", res.ObjectProperty);
        }
    }
}