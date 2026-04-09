using System;
using System.Collections.Generic;
using XamlX;
using Xunit;

namespace XamlParserTests
{
    public class IntrinsicsTestsClass
    {
        public object? ObjectProperty { get; set; }
        public int IntProperty { get; set; }
        public Type? TypeProperty { get; set; }
        public bool BoolProperty { get; set; }
        public bool? NullableBoolProperty { get; set; }

        public static object StaticProp { get; } = "StaticPropValue";
        public static object StaticField = "StaticFieldValue";
        public const string StringConstant = "ConstantValue";
        public const int IntConstant = 100;
        public const float FloatConstant = 2;
        public const double DoubleConstant = 3;
    }

    public class IntrinsicsTestsDerivedClass : IntrinsicsTestsClass;

    public class IntrinsicsTestsGenericClass<T1, T2>
    {
        public static object StaticProp { get; } = "GenericStaticPropValue";
        public static object StaticField = "GenericStaticFieldValue";
        public const string StringConstant = "GenericConstantValue";
    }

    public class IntrinsicsListTestsClass
    {
        internal int AddInt32CallCount;
        internal int AddObjectCallCount;

        public void Add(int value) => ++AddInt32CallCount;
        public void Add(object value) => ++AddObjectCallCount;
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

        [Fact]
        public void Null_Extension_Should_Disregard_Value_Type_Overloads()
        {
            var res = (IntrinsicsListTestsClass) CompileAndRun($@"
<IntrinsicsListTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <x:Null />
    <x:Null />
</IntrinsicsListTestsClass>");

            Assert.Equal(0, res.AddInt32CallCount);
            Assert.Equal(2, res.AddObjectCallCount);
        }

        [Theory,
         InlineData(typeof(IntrinsicsTestsClass), "<x:Type TypeName='IntrinsicsTestsClass' />"),
         InlineData(typeof(Dictionary<string, string>), "<x:Type TypeName='scg:Dictionary(x:String, x:String)' />"),
         InlineData(typeof(Dictionary<string, List<int>>), "<x:Type TypeName='scg:Dictionary(x:String, scg:List(x:Int32))' />"),
         InlineData(typeof(List<string>), "<x:Type x:TypeArguments='x:String' TypeName='scg:List' />"),
         InlineData(typeof(List<List<string>>), "<x:Type x:TypeArguments='scg:List(x:String)' TypeName='scg:List'/>"),
         InlineData(typeof(Dictionary<string, List<int>>), "<x:Type x:TypeArguments='x:String, scg:List(x:Int32)' TypeName='scg:Dictionary' />"),
         InlineData(typeof(int?), "<x:Type TypeName='x:Int32?' />"),
         InlineData(typeof(int?), "<x:Type TypeName='sys:Nullable(x:Int32)' />"),
         InlineData(typeof(string), "<x:Type TypeName='x:String?' />"),
         InlineData(typeof(List<int?>), "<x:Type TypeName='scg:List(x:Int32?)' />"),
        ]
        public void Type_Extension_Resolves_Types(Type expectedType, string typeExt)
        {
            var res = (IntrinsicsTestsClass) CompileAndRun($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
>
    <IntrinsicsTestsClass.TypeProperty>{typeExt}</IntrinsicsTestsClass.TypeProperty>
</IntrinsicsTestsClass>");
            Assert.Equal(expectedType, res.TypeProperty);
        }       
        
        [Theory,
         InlineData(typeof(string), "{x:Type x:String}"),
         InlineData(typeof(string), "{x:Type TypeName=x:String}"),
         InlineData(typeof(Dictionary<string, string>), "{x:Type TypeName='scg:Dictionary(x:String, x:String)'}"),
         InlineData(typeof(Dictionary<string, string>), "{x:Type 'scg:Dictionary(x:String, x:String)'}"),
         InlineData(typeof(Dictionary<string, List<int>>), "{x:Type TypeName='scg:Dictionary(x:String, scg:List(x:Int32))'}"),
         InlineData(typeof(Dictionary<string, List<int>>), "{x:Type 'scg:Dictionary(x:String, scg:List(x:Int32))'}"),
         InlineData(typeof(Dictionary<string, List<int>>), "{x:Type x:TypeArguments='x:String, scg:List(x:Int32)' TypeName='scg:Dictionary' }"),
         InlineData(typeof(int?), "{x:Type x:Int32?}"),
         InlineData(typeof(int?), "{x:Type sys:Nullable(x:Int32)}"),
         InlineData(typeof(string), "{x:Type x:String?}"),
         InlineData(typeof(int?), "{x:Type TypeName=x:Int32?}"),
         InlineData(typeof(List<int?>), "{x:Type TypeName='scg:List(x:Int32?)'}"),
         InlineData(typeof(List<int?>), "{x:Type 'scg:List(x:Int32?)'}"),
         InlineData(typeof(KeyValuePair<int?, string>?), "{x:Type 'scg:KeyValuePair(x:Int32?, x:String?)?'}")
        ]
        public void Type_MarkupExtension_Resolves_Types(Type expectedType, string typeExt)
        {
            var res = (IntrinsicsTestsClass) CompileAndRun($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
    TypeProperty=""{typeExt}"" />");
            Assert.Equal(expectedType, res.TypeProperty);
        }

        [Fact]
        public void Type_Extension_Should_Report_Error_When_TypeName_Generics_Are_Mixed_With_XTypeArguments()
        {
            var ex = Assert.Throws<XamlTransformException>(() => Compile($@"
<IntrinsicsTestsClass
    xmlns='test'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
>
    <IntrinsicsTestsClass.TypeProperty>
        <x:Type TypeName='scg:Dictionary(x:String, x:String)' x:TypeArguments='x:String, x:String' />
    </IntrinsicsTestsClass.TypeProperty>
</IntrinsicsTestsClass>"));

            Assert.Contains("both inline and in x:TypeArguments", ex.Message);
        }

        [Fact]
        public void Type_MarkupExtension_Should_Report_Error_When_TypeName_Generics_Are_Mixed_With_XTypeArguments()
        {
            var ex = Assert.Throws<XamlTransformException>(() => Compile($@"
<IntrinsicsTestsClass
    xmlns='test'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
    TypeProperty=""{{x:Type TypeName='scg:Dictionary(x:String, x:String)', x:TypeArguments='x:String, x:String' }}"" />"));

            Assert.Contains("both inline and in x:TypeArguments", ex.Message);
        }

        [Fact]
        public void Type_Extension_Should_Report_Error_When_Inline_Generic_Syntax_Is_Unbalanced()
        {
            var ex = Assert.Throws<XamlTransformException>(() => Compile($@"
<IntrinsicsTestsClass
    xmlns='test'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
>
    <IntrinsicsTestsClass.TypeProperty>
        <x:Type TypeName='scg:List(x:String' />
    </IntrinsicsTestsClass.TypeProperty>
</IntrinsicsTestsClass>"));

            Assert.Contains("Unable to parse x:Type", ex.Message);
            Assert.Contains("Unmatched '('", ex.Message);
        }

        [Fact]
        public void Type_MarkupExtension_Should_Report_Error_When_Inline_Generic_Syntax_Is_Unbalanced()
        {
            var ex = Assert.Throws<XamlTransformException>(() => Compile($@"
<IntrinsicsTestsClass
    xmlns='test'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
    TypeProperty=""{{x:Type TypeName='scg:List(x:String'}}"" />"));

            Assert.Contains("Unable to parse x:Type", ex.Message);
            Assert.Contains("Unmatched '('", ex.Message);
        }
        
        [Theory,
         // InlineData("<x:Type TypeName='sys:Nullable(x:Int32)?' />"),
         // InlineData("<x:Type TypeName='sys:Nullable(x:Int32?)' />"),
         // InlineData("<x:Type TypeName='sys:Nullable(sys:Nullable(x:Int32))' />"),
         InlineData("<x:Type TypeName='x:Int32??' />"),
         InlineData("<x:Type TypeName='x:String??' />")
        ]
        public void Type_Extension_Should_Report_Error_When_Nullable_Is_Applied_To_Nullable(string typeExt)
        {
            var ex = Assert.Throws<XamlTransformException>(() => Compile($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
>
    <IntrinsicsTestsClass.TypeProperty>{typeExt}</IntrinsicsTestsClass.TypeProperty>
</IntrinsicsTestsClass>"));
            Assert.Contains("multiple nullable indicators", ex.Message);
        }
        

        [Theory,
         InlineData("StaticPropValue", "<x:Static Member='IntrinsicsTestsClass.StaticProp'/>"),
         InlineData("StaticFieldValue", "<x:Static Member='IntrinsicsTestsClass.StaticField'/>"),
         InlineData("ConstantValue", "<x:Static Member='IntrinsicsTestsClass.StringConstant'/>"),
         InlineData(100, "<x:Static Member='IntrinsicsTestsClass.IntConstant'/>"),
         InlineData(2f, "<x:Static Member='IntrinsicsTestsClass.FloatConstant'/>"),
         InlineData(3d, "<x:Static Member='IntrinsicsTestsClass.DoubleConstant'/>"),
         InlineData("StaticPropValue", "<x:Static Member='IntrinsicsTestsDerivedClass.StaticProp'/>"),
         InlineData("StaticFieldValue", "<x:Static Member='IntrinsicsTestsDerivedClass.StaticField'/>"),
         InlineData("GenericStaticPropValue", "<x:Static Member='IntrinsicsTestsGenericClass(x:String, x:String).StaticProp'/>"),
         InlineData("GenericStaticPropValue", "<x:Static Member='IntrinsicsTestsGenericClass.StaticProp' x:TypeArguments='x:String, x:String'/>"),
         InlineData("GenericStaticFieldValue", "<x:Static Member='IntrinsicsTestsGenericClass(x:String, scg:List(x:Int32)).StaticField'/>"),
         InlineData("GenericStaticFieldValue", "<x:Static Member='IntrinsicsTestsGenericClass.StaticField'  x:TypeArguments='x:String, scg:List(x:Int32)'/>"),
         InlineData("GenericConstantValue", "<x:Static Member='IntrinsicsTestsGenericClass(x:String, x:String).StringConstant'/>"),
         InlineData("GenericConstantValue", "<x:Static Member='IntrinsicsTestsGenericClass.StringConstant' x:TypeArguments='x:String, x:String'/>"),
         InlineData("GenericStaticPropValue", "<x:Static Member='IntrinsicsTestsGenericClass(x:Int32?, x:String).StaticProp'/>"),
         InlineData("GenericStaticFieldValue", "<x:Static Member='IntrinsicsTestsGenericClass(x:Int32?, scg:List(x:Int32)).StaticField'/>")
        ]
        public void Static_Extension_Resolves_Values(object expected, string r)
        {
            var res = (IntrinsicsTestsClass) CompileAndRun($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
>
    <IntrinsicsTestsClass.ObjectProperty>{r}</IntrinsicsTestsClass.ObjectProperty>
</IntrinsicsTestsClass>");
            Assert.Equal(expected, res.ObjectProperty);
        }

        [Theory,
         InlineData("StaticPropValue", "{x:Static Member='IntrinsicsTestsClass.StaticProp'}"),
         InlineData("StaticFieldValue", "{x:Static Member='IntrinsicsTestsClass.StaticField'}"),
         InlineData("ConstantValue", "{x:Static Member='IntrinsicsTestsClass.StringConstant'}"),
         InlineData("GenericStaticPropValue", "{x:Static Member='IntrinsicsTestsGenericClass(x:String, x:String).StaticProp'}"),
         InlineData("GenericStaticPropValue", "{x:Static x:TypeArguments='x:String, x:String', Member='IntrinsicsTestsGenericClass.StaticProp'}"),
         InlineData("GenericStaticFieldValue", "{x:Static Member='IntrinsicsTestsGenericClass(x:String, scg:List(x:Int32)).StaticField'}"),
         InlineData("GenericStaticFieldValue", "{x:Static x:TypeArguments='x:String, scg:List(x:Int32)', Member='IntrinsicsTestsGenericClass.StaticField'}"),
         InlineData("GenericConstantValue", "{x:Static Member='IntrinsicsTestsGenericClass(x:String, x:String).StringConstant'}"),
         InlineData("GenericConstantValue", "{x:Static x:TypeArguments='x:String, x:String', Member='IntrinsicsTestsGenericClass.StringConstant'}"),
         InlineData("GenericStaticPropValue", "{x:Static Member='IntrinsicsTestsGenericClass(x:Int32?, x:String).StaticProp'}")]
        public void Static_MarkupExtension_Resolves_Values(object expected, string markup)
        {
            var res = (IntrinsicsTestsClass) CompileAndRun($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
    ObjectProperty=""{markup}"" />");
            Assert.Equal(expected, res.ObjectProperty);
        }

        [Fact]
        public void Static_Extension_Reports_Errors()
        {
            var exception = Assert.Throws<AggregateException>(() => CompileAndRun($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic'
>
    <IntrinsicsTestsClass.ObjectProperty><x:Static Member='IntrinsicsTestsClass.StaticPropDoesntExist1'/></IntrinsicsTestsClass.ObjectProperty>
    <IntrinsicsTestsClass.BoolProperty><x:Static Member='IntrinsicsTestsClass.StaticPropDoesntExist2'/></IntrinsicsTestsClass.BoolProperty>
</IntrinsicsTestsClass>"));
            
            Assert.Equal(2, exception.InnerExceptions.Count);
            Assert.Collection(exception.InnerExceptions,
                ex1 => Assert.Contains("StaticPropDoesntExist1", ex1.Message),
                ex2 => Assert.Contains("StaticPropDoesntExist2", ex2.Message));
        }

        [Fact]
        public void Static_Extension_Should_Report_Error_When_Member_Generics_Are_Mixed_With_XTypeArguments()
        {
            var ex = Assert.Throws<XamlTransformException>(() => Compile($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
>
    <IntrinsicsTestsClass.ObjectProperty>
        <x:Static Member='IntrinsicsTestsGenericClass(x:String, x:String).StaticProp' x:TypeArguments='x:String, x:String' />
    </IntrinsicsTestsClass.ObjectProperty>
</IntrinsicsTestsClass>"));

            Assert.Contains("both inline and in x:TypeArguments", ex.Message);
        }

        [Fact]
        public void Static_MarkupExtension_Should_Report_Error_When_Member_Generics_Are_Mixed_With_XTypeArguments()
        {
            var ex = Assert.Throws<XamlTransformException>(() => Compile($@"
<IntrinsicsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:scg='clr-namespace:System.Collections.Generic;assembly=netstandard'
    ObjectProperty=""{{x:Static Member='IntrinsicsTestsGenericClass(x:String, x:String).StaticProp', x:TypeArguments='x:String, x:String' }}"" />"));

            Assert.Contains("both inline and in x:TypeArguments", ex.Message);
        }
        
        [Fact]
        public void Static_Extension_Resolves_Enum_Values()
        {
            Static_Extension_Resolves_Values(IntrinsicsTestsEnum.Foo, "<x:Static Member='IntrinsicsTestsEnum.Foo'/>");
        }

        [Theory,
            InlineData(true, "x:True"),
            InlineData(false, "x:False")]
        public void Boolean_Extension_Can_Be_Set_To_Object(bool expected, string value)
        {
            var res = (IntrinsicsTestsClass)CompileAndRun($@"
<IntrinsicsTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <IntrinsicsTestsClass.ObjectProperty><{value}/></IntrinsicsTestsClass.ObjectProperty>
</IntrinsicsTestsClass>");
            Assert.Equal(expected, res.ObjectProperty);
        }

        [Theory,
            InlineData(true, "x:True"),
            InlineData(false, "x:False")]
        public void Boolean_Extension_Can_Be_Set_To_Bool(bool expected, string value)
        {
            var res = (IntrinsicsTestsClass)CompileAndRun($@"
<IntrinsicsTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <IntrinsicsTestsClass.BoolProperty><{value}/></IntrinsicsTestsClass.BoolProperty>
</IntrinsicsTestsClass>");
            Assert.Equal(expected, res.BoolProperty);
        }

        [Theory,
            InlineData(true, "x:True"),
            InlineData(false, "x:False")]
        public void Boolean_Extension_Can_Be_Set_To_NullableBool(bool expected, string value)
        {
            var res = (IntrinsicsTestsClass)CompileAndRun($@"
<IntrinsicsTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <IntrinsicsTestsClass.NullableBoolProperty><{value}/></IntrinsicsTestsClass.NullableBoolProperty>
</IntrinsicsTestsClass>");
            Assert.Equal(expected, res.NullableBoolProperty);
        }

        [Theory,
            InlineData(true, "x:True"),
            InlineData(false, "x:False")]
        public void Boolean_Extension_Can_Be_Used_As_Markup_Ext(bool expected, string value)
        {
            var res = (IntrinsicsTestsClass)CompileAndRun($@"
<IntrinsicsTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                      ObjectProperty='{{{value}}}' />");
            Assert.Equal(expected, res.ObjectProperty);
        }

        [Fact]
        public void Boolean_Extension_Should_Cause_Compilation_Error_When_Applied_To_Wrong_Type()
        {
            Assert.Throws<XamlLoadException>(() => Compile(@"
<IntrinsicsTestsClass xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                      IntProperty='{x:True}' />"));
        }
    }
}
