using System;
using System.Collections.Generic;
using Xunit;

namespace XamlParserTests
{
    public class MarkupExtensionTestsClass
    {
        public int IntProperty { get; set; }
        public int NullableIntProperty { get; set; }
        public string StringProperty { get; set; }
        public object ObjectProperty { get; set; }
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
    
    public class MarkupExtensionTests : CompilerTestBase
    {
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
        
        IServiceProvider CreateValueProvider(object value)=>new DictionaryServiceProvider
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
        public void Object_Should_Be_Casted_To_NullableValue_Type()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' 
    NullableIntProperty='{ServiceProviderValue}'/>", CreateValueProvider(123));
            Assert.Equal(123, res.NullableIntProperty);
        }
        
        /*TODO: checks for
         
         overload with IServiceProvider
         
         Various conversions:
         
         success:
           value type to nullable
           value type to object
           value type exact
           instance type to value type
           
         external:
           value type to value type
           value type to instance type
           instance type to value type
           instance type to instance type
        */
    }
}