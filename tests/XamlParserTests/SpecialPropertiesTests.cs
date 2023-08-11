using Xunit;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}

namespace XamlParserTests
{
    public class InitPropertiesTestClass
    {
        public string Prop1 { get; init; }
        public int Prop2 { get; init; }
    }

    public class SpecialPropertiesTests : CompilerTestBase
    {
        [Fact]
        public void Init_Properties_Should_Be_Set()
        {
            var result = (InitPropertiesTestClass) CompileAndRun(
                "<InitPropertiesTestClass xmlns='clr-namespace:XamlParserTests' Prop1='foo' Prop2='42' />");
            Assert.Equal("foo", result.Prop1);
            Assert.Equal(42, result.Prop2);
        }
    }
}
