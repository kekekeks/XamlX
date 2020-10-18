using System.Linq;
using Xunit;

namespace XamlParserTests
{
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