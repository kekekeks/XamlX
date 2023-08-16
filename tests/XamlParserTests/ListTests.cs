using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace XamlParserTests
{
    public class EnumerableContentClass
    {
        [Content]
        public IEnumerable<SimpleSubClass> Children { get; set; } = new List<SimpleSubClass>();
    }

   
    public class ListTests : CompilerTestBase
    {
        [Fact]
        public void Enumerable_Properties_Should_Be_Treated_As_Lists()
        {
            var res = (EnumerableContentClass)CompileAndRun(@"
<EnumerableContentClass xmlns='test'>
    <SimpleSubClass Test='test'/>
    <SimpleSubClass Test='test2'/>
    
</EnumerableContentClass>");

            
            Assert.Equal("test", res.Children.First().Test);
            Assert.Equal("test2", res.Children.Last().Test);
        }
    }


}