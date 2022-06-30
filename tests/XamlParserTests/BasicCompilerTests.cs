using System;
using System.Collections.Generic;
using XamlX;
using Xunit;

namespace XamlParserTests
{
    public class SimpleClass
    {
        public string Test { get; set; }
        public string Test2 { get; set; }
        [Content]
        public List<SimpleSubClass> Children { get; set; } = new List<SimpleSubClass>();
    }

    public class SimpleSubClass
    {
        public string Test { get; set; }
    }
    
    public class ObjectWithAddChild : IAddChild
    {
        public object Child { get; private set; }

        void IAddChild.AddChild(object child)
        {
            Child = child;
        }
    }

    public class ObjectWithGenericAddChild : IAddChild<string>
    {
        public object Child { get; private set; }

        public string Text { get; private set; }

        void IAddChild.AddChild(object child)
        {
            Child = child;
        }

        void IAddChild<string>.AddChild(string text)
        {
            Text = text;
        }
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

            var res = populate ? new SimpleClass() : (SimpleClass) comp.create(null);
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

            var res = (ObjectWithAddChild)comp.create(null);

            comp.populate(null, res);

            Assert.Equal("123", res.Child);
        }

        [Fact]
        public void Compiler_Should_Compile_Xaml_With_Generic_IAddChild()
        {
            var comp = Compile(@"<ObjectWithGenericAddChild xmlns='test'  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>123</ObjectWithGenericAddChild>");

            var res = (ObjectWithGenericAddChild)comp.create(null);

            comp.populate(null, res);

            Assert.Null(res.Child);

            Assert.Equal("123", res.Text);
        }
    }
}