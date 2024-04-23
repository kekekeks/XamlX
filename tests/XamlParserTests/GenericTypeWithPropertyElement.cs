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
            
            var res = (RootNode)comp.create(null);
            //comp.populate(null, res);
            
            Assert.NotNull(res);
            Assert.NotNull(res.Children);
            Assert.Equal(res.Children.Count, 1);
            var child1 = res.Children[0];
            Assert.IsType(typeof(GenericClass<TypeArgument>), res.Children[0]);
            GenericClass<TypeArgument> child = res.Children[0] as GenericClass<TypeArgument>;
            Assert.NotNull(child.Items);
            Assert.Equal(child.Items.Count, 1);
            Assert.IsType(typeof(ItemNode), child.Items[0]);
        }    
    }
}