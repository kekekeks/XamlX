using System;
using System.Collections.Generic;
using XamlX;
using Xunit;

namespace XamlParserTests
{
    public class MarkupExtensionTestsClass
    {
        public int IntProperty { get; set; }
        public double DoubleProperty { get; set; }
        public int? NullableIntProperty { get; set; }
        public string? StringProperty { get; set; }
        public object? ObjectProperty { get; set; }
        [Content]
        public List<int> IntList { get; } = new List<int>();
        public List<int> IntList2 { get; set; } = new List<int>();
        public List<int> ReadOnlyIntList { get; } = new List<int>();
    }

    public class MarkupExtensionContentDictionaryClass
    {
        [Content]
        public Dictionary<string, int> IntDic { get; } = new Dictionary<string, int>();
    }

    public class ObjectTestExtension
    {
        public object? Returned { get; set; }
        public object? ProvideValue()
        {
            return Returned;
        }
    }

    // Shouldn't be conflicted with actual generic types with the same name.
    public class GenericTestExtension
    {
        public object? Returned { get; set; }
        public object? ProvideValue()
        {
            return Returned;
        }
    }

    public class GenericTestExtension<TType>
    {
        public TType Returned { get; set; } = default!;
        public object? ProvideValue()
        {
            return Returned;
        }
    }

    public class GenericTestExtension<TType1, TType2>
    {
        public TType1 Returned1 { get; set; } = default!;
        public TType2 Returned2 { get; set; } = default!;
        public object? ProvideValue()
        {
            return (Returned1, Returned2);
        }
    }

    class DictionaryServiceProvider : Dictionary<Type, object?>, IServiceProvider
    {
        public IServiceProvider? Parent { get; set; }
        public object? GetService(Type serviceType)
        {
            if(TryGetValue(serviceType, out var impl))
               return impl;
            return Parent?.GetService(serviceType);
        }
    }

    public class ExtensionValueHolder
    {
        public object? Value { get; set; }
    }

    public class ServiceProviderValueExtension
    {
        public object? ProvideValue(IServiceProvider sp) =>
            ((ExtensionValueHolder) sp.GetService(typeof(ExtensionValueHolder))!).Value;
    }

    public class ServiceProviderIntValueExtension
    {
        public int ProvideValue(IServiceProvider sp) =>
            (int) ((ExtensionValueHolder) sp.GetService(typeof(ExtensionValueHolder))!).Value!;
    }

    public class ServiceProviderIntListExtension
    {
        public List<int> ProvideValue(IServiceProvider sp) =>
            new() { (int)((ExtensionValueHolder)sp.GetService(typeof(ExtensionValueHolder))!).Value! };
    }

    public class CustomConvertedType
    {
        public string? Value { get; set; }
    }



    public class MarkupExtensionTests : CompilerTestBase
    {
        [Fact]
        public void Object_Should_Be_Casted_To_String()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' StringProperty='{ObjectTestExtension Returned=test}'/>");
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
        public void Extensions_Should_Be_Able_To_Assign_Lists()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' IntList2='{ServiceProviderIntList}'>
</MarkupExtensionTestsClass>", CreateValueProvider(123));
            Assert.Equal(new[] {123}, res.IntList2);
        }

        [Fact]
        public void Extensions_Which_Dont_Return_Collections_Should_Not_Be_Able_To_Assign_Lists()
        {
            Assert.Throws<InvalidCastException>(() => CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' IntList2='{ServiceProviderValue}'>
</MarkupExtensionTestsClass>", CreateValueProvider(123)));
        }

        [Fact]
        public void Extensions_Should_Not_Be_Able_To_Assign_To_ReadOnly_Lists()
        {
            Assert.Throws<XamlLoadException>(() => Compile(@"
<MarkupExtensionTestsClass xmlns='test' ReadOnlyIntList='{ServiceProviderIntList}'>
</MarkupExtensionTestsClass>"));
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

        [Fact]
        public void Markup_Extension_With_Directive_Should_Compile()
        {
            // Compiler throws an error if there is undefined directive.
            // This way we can check if this directive was even parsed.
            var ex = Assert.Throws<XamlLoadException>(() => CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    StringProperty='{ObjectTestExtension x:Dir=RTL}'/>"));
            Assert.Contains("XamlX.Ast.XamlAstXmlDirective", ex.Message);
        }

        [Fact]
        public void Same_Name_Extension_Should_Work_Without_Generics()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' ObjectProperty='{GenericTestExtension Returned=test}'/>");
            Assert.Equal("test", res.ObjectProperty);
        }

        [Fact]
        public void Same_Name_Extension_Should_Work_Without_Generics_XML_Syntax()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <MarkupExtensionTestsClass.ObjectProperty>
        <GenericTestExtension Returned='test' />
    </MarkupExtensionTestsClass.ObjectProperty>
</MarkupExtensionTestsClass>");
            Assert.Equal("test", res.ObjectProperty);
        }

        [Fact]
        public void Resolve_Single_Generic_Type_Argument()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    IntProperty='{GenericTestExtension Returned=5, x:TypeArguments=x:Int32}'/>");
            Assert.Equal(5, res.IntProperty);
        }

        [Fact]
        public void Resolve_Single_Generic_Type_Argument_XML_Syntax()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <MarkupExtensionTestsClass.IntProperty>
        <GenericTestExtension Returned='5' x:TypeArguments='x:Int32' />
    </MarkupExtensionTestsClass.IntProperty>
</MarkupExtensionTestsClass>");
            Assert.Equal(5, res.IntProperty);
        }

        [Fact]
        public void Resolve_Double_Generic_Type_Argument()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    ObjectProperty='{GenericTestExtension Returned1=5, Returned2=0.4, x:TypeArguments=""x:Int32,x:Single""}'/>");
            Assert.Equal((5, 0.4f), (ValueTuple<int, float>)res.ObjectProperty!);
        }

        [Fact]
        public void Resolve_Double_Generic_Type_Argument_XML_Syntax()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <MarkupExtensionTestsClass.ObjectProperty>
        <GenericTestExtension Returned1='5' Returned2='0.4' x:TypeArguments='x:Int32,x:Single' />
    </MarkupExtensionTestsClass.ObjectProperty>
</MarkupExtensionTestsClass>");
            Assert.Equal((5, 0.4f), (ValueTuple<int, float>)res.ObjectProperty!);
        }

        [Fact]
        public void Resolve_Single_Generic_Type_Argument_With_Nested_Extension()
        {
            var res = (MarkupExtensionTestsClass) CompileAndRun(@"
<MarkupExtensionTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    StringProperty='{GenericTestExtension Returned={GenericTestExtension Returned=test, x:TypeArguments=x:String}, x:TypeArguments=x:Object}'/>");
            Assert.Equal("test", res.StringProperty);
        }
    }
}
