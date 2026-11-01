using XamlX.Transform;
using Xunit;

namespace XamlParserTests
{
    public class GeneratedIdentifierTests
    {
        [Fact]
        public void ExistingGeneratorsUseReadableNames()
        {
            IXamlIdentifierGenerator generator = new SequentialGenerator();

            var prefixed = generator.CreateIdentifierFromPrefix("XamlClosure_");
            var preferred = generator.CreateIdentifierFromPreferredName("Property", 0);
            var duplicate = generator.CreateIdentifierFromPreferredName("Property", 1);

            string prefixedName = prefixed;
            Assert.Equal("XamlClosure_1", prefixedName);
            Assert.Equal("Property!Field", preferred.AppendSuffix("!Field"));
            Assert.Equal("Property_2!Getter", duplicate.AppendSuffix("!Getter"));
            Assert.Equal("Property_2!Setter", duplicate.AppendSuffix("!Setter"));
        }

        private sealed class SequentialGenerator : IXamlIdentifierGenerator
        {
            private int _next = 1;

            public string GenerateIdentifierPart() => (_next++).ToString();
        }
    }
}
