using System;
using System.Collections.Generic;
using Xunit;

namespace XamlParserTests
{
    public class SimpleClass
    {
        public string? Test { get; set; }
        public string? Test2 { get; set; }
        [Content]
        public List<SimpleSubClass> Children { get; set; } = new List<SimpleSubClass>();
    }

    public class SimpleSubClass
    {
        public string? Test { get; set; }
    }
    
    public class ObjectWithAddChild : IAddChild
    {
        public object? Child { get; private set; }

        void IAddChild.AddChild(object child)
        {
            Child = child;
        }
    }

    public class ObjectWithGenericAddChild : IAddChild<string>
    {
        public object? Child { get; private set; }

        public string? Text { get; private set; }

        void IAddChild.AddChild(object child)
        {
            Child = child;
        }

        void IAddChild<string>.AddChild(string text)
        {
            Text = text;
        }
    }

    public class ObjectWithoutMatchingCtor
    {
        public ObjectWithoutMatchingCtor(string? param)
        {
            Arg = param;
        }
        
        public string? Arg { get; set; }
        public string? Prop { get; set; }
    }
    
    public class ObjectWithPrivateCtor
    {
        private ObjectWithPrivateCtor(string? param)
        {
            Arg = param;
        }

        public static ObjectWithPrivateCtor Factory(string? param) => new(param);
        
        public string? Arg { get; set; }
        public string? Prop { get; set; }
    }
    
    public class BasicCompilerTests : CompilerTestBase
    {
        [Theory,
            InlineData(false),
            InlineData(true)]
        public void Compiler_Should_Compile_Simple_Xaml(bool populate)
        {
            var comp = Compile(@"
<SimpleClass xmlns='test' Test='123'>
    <SimpleSubClass Test='test'/>
    <SimpleClass.Test2>321</SimpleClass.Test2>
    <SimpleSubClass Test='test2'/>
    
</SimpleClass>");

            var res = populate ? new SimpleClass() : (SimpleClass) comp.create!(null);
            if (populate)
                comp.populate(null, res);
            
            Assert.Equal("123", res.Test);
            Assert.Equal("321", res.Test2);
            Assert.Equal("test", res.Children[0].Test);
            Assert.Equal("test2", res.Children[1].Test);
        }    

        [Fact]
        public void Compiler_Should_Compile_Xaml_With_IAddChild()
        {
            var comp = Compile(@"<ObjectWithAddChild xmlns='test'  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>123</ObjectWithAddChild>");

            var res = (ObjectWithAddChild)comp.create!(null);

            comp.populate(null, res);

            Assert.Equal("123", res.Child);
        }

        [Fact]
        public void Compiler_Should_Compile_Xaml_With_Generic_IAddChild()
        {
            var comp = Compile(@"<ObjectWithGenericAddChild xmlns='test'  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>123</ObjectWithGenericAddChild>");

            var res = (ObjectWithGenericAddChild)comp.create!(null);

            comp.populate(null, res);

            Assert.Null(res.Child);

            Assert.Equal("123", res.Text);
        }
        
        [Fact]
        public void Compiler_Should_Populate_Xaml_Without_MatchingCtor()
        {
            var comp = Compile(@"
<ObjectWithoutMatchingCtor xmlns='test' Prop='321' />", generateBuildMethod: false);

            var res = new ObjectWithoutMatchingCtor("123");
            comp.populate(null, res);
            
            Assert.Equal("123", res.Arg);
            Assert.Equal("321", res.Prop);
        }

        [Fact]
        public void Compiler_Should_Fail_To_Build_Xaml_Without_MatchingCtor()
        {
            Assert.Throws<InvalidOperationException>(() => Compile(@"
<ObjectWithoutMatchingCtor xmlns='test' Prop='321' />"));
        }
        
        [Fact]
        public void Compiler_Should_Populate_Xaml_Without_Public_Ctor()
        {
            var comp = Compile(@"
<ObjectWithPrivateCtor xmlns='test' Prop='321' />", generateBuildMethod: false);

            var res = ObjectWithPrivateCtor.Factory("123");
            comp.populate(null, res);
            
            Assert.Equal("123", res.Arg);
            Assert.Equal("321", res.Prop);
        }

        [Fact]
        public void Compiler_Should_Fail_To_Build_Xaml_Without_Public_Ctor()
        {
            Assert.Throws<InvalidOperationException>(() => Compile(@"
<ObjectWithPrivateCtor xmlns='test' Prop='321' />"));
        }
    }
}
