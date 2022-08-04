using System;
using Xunit;

namespace XamlParserTests
{
    public class DynamicSettersClass<T1, T2>
    {
        internal bool AddT1Called;
        internal T1 T1Result;

        internal bool AddT2Called;
        internal T2 T2Result;

        public void Add(T1 value)
        {
            AddT1Called = true;
            T1Result = value;
        }

        public void Add(T2 value)
        {
            AddT2Called = true;
            T2Result = value;
        }
    }

    public class DynamicProvider
    {
        public enum ProvidedValueType { Null, String, Uri, Int32, TimeSpan, DateTime }

        public ProvidedValueType ProvidedValue { get; set; }

        public object ProvideValue()
        {
            return ProvidedValue switch
            {
                ProvidedValueType.Null => null,
                ProvidedValueType.String => "foo",
                ProvidedValueType.Uri => new Uri("https://avaloniaui.net/"),
                ProvidedValueType.Int32 => 1234,
                ProvidedValueType.TimeSpan => new TimeSpan(12, 34, 56, 789),
                ProvidedValueType.DateTime => DateTime.Now,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public class DynamicSettersTests : CompilerTestBase
    {
        [Fact]
        public void Dynamic_Setter_With_Reference_Types_And_Null_Argument_Should_Match_Any()
        {
            var result = (DynamicSettersClass<string, Uri>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:String,sys:Uri' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Null' />
</DynamicSettersClass>
");
            // we can't be sure that AddT1 is always called because it's first: metadata order isn't guaranteed
            Assert.True(result.AddT1Called ^ result.AddT2Called);
            Assert.Null(result.T1Result);
            Assert.Null(result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Reference_Types_And_Typed_Argument_Should_Match_1()
        {
            var result = (DynamicSettersClass<string, Uri>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:String,sys:Uri' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='String' />
</DynamicSettersClass>
");
            Assert.True(result.AddT1Called);
            Assert.Equal("foo", result.T1Result);
            Assert.False(result.AddT2Called);
            Assert.Null(result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Reference_Types_And_Typed_Argument_Should_Match_2()
        {
            var result = (DynamicSettersClass<string, Uri>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:String,sys:Uri' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Uri' />
</DynamicSettersClass>
");
            Assert.False(result.AddT1Called);
            Assert.Null(result.T1Result);
            Assert.True(result.AddT2Called);
            Assert.Equal(new Uri("https://avaloniaui.net/"), result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Types_And_Null_Argument_Should_Throw_NullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:TimeSpan' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Null' />
</DynamicSettersClass>
"));
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Types_And_Typed_Argument_Should_Match_1()
        {
            var result = (DynamicSettersClass<int, TimeSpan>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:TimeSpan' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Int32' />
</DynamicSettersClass>
");
            Assert.True(result.AddT1Called);
            Assert.Equal(1234, result.T1Result);
            Assert.False(result.AddT2Called);
            Assert.Equal(default, result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Types_And_Typed_Argument_Should_Match_2()
        {
            var result = (DynamicSettersClass<int, TimeSpan>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:TimeSpan' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='TimeSpan' />
</DynamicSettersClass>
");
            Assert.False(result.AddT1Called);
            Assert.Equal(default, result.T1Result);
            Assert.True(result.AddT2Called);
            Assert.Equal(new TimeSpan(12, 34, 56, 789), result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Types_And_Null_Argument_Should_Match_Any()
        {
            var result = (DynamicSettersClass<int?, TimeSpan?>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:Nullable(sys:TimeSpan)' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Null' />
</DynamicSettersClass>
");
            // we can't be sure that AddT1 is always called because it's first: metadata order isn't guaranteed
            Assert.True(result.AddT1Called ^ result.AddT2Called);
            Assert.Null(result.T1Result);
            Assert.Null(result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Types_And_Typed_Argument_Should_Match_1()
        {
            var result = (DynamicSettersClass<int?, TimeSpan?>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:Nullable(sys:TimeSpan)' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Int32' />
</DynamicSettersClass>
");
            Assert.True(result.AddT1Called);
            Assert.Equal(1234, result.T1Result);
            Assert.False(result.AddT2Called);
            Assert.Equal(default, result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Types_And_Typed_Argument_Should_Match_2()
        {
            var result = (DynamicSettersClass<int?, TimeSpan?>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:Nullable(sys:TimeSpan)' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='TimeSpan' />
</DynamicSettersClass>
");
            Assert.False(result.AddT1Called);
            Assert.Equal(default, result.T1Result);
            Assert.True(result.AddT2Called);
            Assert.Equal(new TimeSpan(12, 34, 56, 789), result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Type_And_Reference_Type_And_Null_Argument_Should_Match_Reference()
        {
            var result = (DynamicSettersClass<int, string>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Null' />
</DynamicSettersClass>
");
            Assert.False(result.AddT1Called);
            Assert.Equal(default, result.T1Result);
            Assert.True(result.AddT2Called);
            Assert.Null(result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Type_And_Reference_Type_And_Value_Type_Argument_Should_Match_Value_Type()
        {
            var result = (DynamicSettersClass<int, string>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Int32' />
</DynamicSettersClass>
");
            Assert.True(result.AddT1Called);
            Assert.Equal(1234, result.T1Result);
            Assert.False(result.AddT2Called);
            Assert.Null(result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Type_And_Reference_Type_And_Null_Argument_Should_Match_Any()
        {
            var result = (DynamicSettersClass<int?, string>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Null' />
</DynamicSettersClass>
");
            // we can't be sure that AddT1 is always called because it's first: metadata order isn't guaranteed
            Assert.True(result.AddT1Called ^ result.AddT2Called);
            Assert.Null(result.T1Result);
            Assert.Null(result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Type_And_Reference_Type_And_Value_Type_Argument_Should_Match_Nullable_Value_Type()
        {
            var result = (DynamicSettersClass<int?, string>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='Int32' />
</DynamicSettersClass>
");
            Assert.True(result.AddT1Called);
            Assert.Equal(1234, result.T1Result);
            Assert.False(result.AddT2Called);
            Assert.Null(result.T2Result);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Type_And_Reference_Type_And_Reference_Type_Argument_Should_Match_Reference_Type()
        {
            var result = (DynamicSettersClass<int?, string>) CompileAndRun(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='String' />
</DynamicSettersClass>
");
            Assert.False(result.AddT1Called);
            Assert.Equal(default, result.T1Result);
            Assert.True(result.AddT2Called);
            Assert.Equal("foo", result.T2Result);
        }

        [Theory]
        [InlineData("sys:Int32", "sys:TimeSpan")]
        [InlineData("sys:String", "sys:Uri")]
        [InlineData("sys:Nullable(sys:Int32)", "sys:Nullable(sys:TimeSpan)")]
        [InlineData("sys:Int32", "sys:String")]
        [InlineData("sys:Nullable(sys:Int32)", "sys:String")]
        public void Dynamic_Setter_With_Mismatched_Argument_Should_Throw_InvalidCastException(string type1, string type2)
        {
            Assert.Throws<InvalidCastException>(() => CompileAndRun($@"
<DynamicSettersClass 
    x:TypeArguments='{type1},{type2}' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <DynamicProvider ProvidedValue='DateTime' />
</DynamicSettersClass>
"));
        }
    }
}
