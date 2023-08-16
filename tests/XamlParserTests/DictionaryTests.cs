using System.Collections.Generic;
using Xunit;

namespace XamlParserTests
{
    public class SimpleClassWithDictionaryContent
    {
        public string Test { get; set; }
        [Content]
        public Dictionary<object, object> Children { get; set; } = new Dictionary<object, object>();
        
        public Dictionary<object, object> NonContentChildren { get; set; } = new Dictionary<object, object>();
    }
    
    
    public class DictionaryTests : CompilerTestBase
    {
        [Fact]
        public void Compiler_Should_Be_Able_To_Populate_Dictionary_Content()
        {
            var res = CompileAndRun(@"
<SimpleClassWithDictionaryContent xmlns='test'  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SimpleClassWithDictionaryContent Test='123' x:Key='test'/>
    <SimpleClassWithDictionaryContent Test='321' x:Key='{x:Type SimpleClassWithDictionaryContent}'/>
    <SimpleClassWithDictionaryContent.NonContentChildren>
        <SimpleClassWithDictionaryContent Test='ch' x:Key='test2'/>
    </SimpleClassWithDictionaryContent.NonContentChildren>
</SimpleClassWithDictionaryContent>");

            Helpers.StructDiff(res, new SimpleClassWithDictionaryContent
            {
                Children =
                {
                    ["test"] = new SimpleClassWithDictionaryContent {Test = "123"},
                    [typeof(SimpleClassWithDictionaryContent)] = new SimpleClassWithDictionaryContent {Test = "321"}
                },
                NonContentChildren =
                {
                    ["test2"] = new SimpleClassWithDictionaryContent {Test = "ch"}
                }
            });
        }
    }
}