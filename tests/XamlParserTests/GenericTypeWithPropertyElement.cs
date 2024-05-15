using System.Collections.Generic;
using Xunit;

namespace XamlParserTests
{
    public class RootNode
    {
        [Content] public List<object> Children { get; set; } = new List<object>();
    }
    
    public class TypeArgument
    {
    
    }
    
    public class ItemNode
    {
    
    }
    
    public class GenericClass<T>
    {
        [Content]
        public List<object> Items { get; set; } = new List<object>();
    }
    
    public class GenericTypeWithPropertyElementCompilerTests : CompilerTestBase
    {
        [Fact]
        public void GenericTypewithPropertyElementTest()
        {
            var comp = Compile(@"
    <RootNode xmlns='test' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>           
        <GenericClass x:TypeArguments='TypeArgument'>
            <GenericClass.Items>
                <ItemNode />  
            </GenericClass.Items>
        </GenericClass>
    </RootNode>");
            
            var res = (RootNode)comp.create!(null);
            
            Assert.NotNull(res);
            Assert.NotNull(res.Children);
            var untypedChild = Assert.Single(res.Children);
            var typedChild = Assert.IsType<GenericClass<TypeArgument>>(untypedChild);
            Assert.NotNull(typedChild.Items);
            var item = Assert.Single(typedChild.Items);
            Assert.IsType<ItemNode>(item);
        }    
    }
}