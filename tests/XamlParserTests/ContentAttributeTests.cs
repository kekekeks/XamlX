using XamlX;
using Xunit;

namespace XamlParserTests
{
    public class SimpleClassWithContentAttribute
    {
        [Content]
        public string? Text { get; set; }
    }

    public class SubClassWithContentAttributeOverride : SimpleClassWithContentAttribute
    {
        [Content]
        public string? OtherText { get; set; }
    }

    public class SimpleClassWithTwoContentAttributes
    {
        [Content]
        public string? Text { get; set; }

        [Content]
        public string? OtherText { get; set; }
    }

    public class ContentAttributeTests : CompilerTestBase
    {
        
        [Fact]
        public void Compiler_Should_Support_ContentAttribute()
        {
            var comp = Compile(@"<SimpleClassWithContentAttribute xmlns='test'  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>123</SimpleClassWithContentAttribute>");

            var res = (SimpleClassWithContentAttribute)comp.create!(null);

            comp.populate(null, res);

            Assert.Equal("123", res.Text);
        }

        [Fact]
        public void Compiler_Should_Support_ContentAttribute_Override()
        {
            var comp = Compile(@"<SubClassWithContentAttributeOverride xmlns='test'  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>123</SubClassWithContentAttributeOverride>");

            var res = (SubClassWithContentAttributeOverride)comp.create!(null);

            comp.populate(null, res);

            Assert.Null(res.Text);
            Assert.Equal("123", res.OtherText);
        }
        
        [Fact]
        public void Compiler_Should_Fail_To_Build_Xaml_When_Multiple_ContentAttributes_Defined()
        {
            Assert.Throws<XamlTransformException>(() => Compile(@"
<SimpleClassWithTwoContentAttributes xmlns='test'>123</SimpleClassWithTwoContentAttributes>"));
        }
    }
}
