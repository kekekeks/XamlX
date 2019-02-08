using System;
using System.Collections.Generic;
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
    }


}