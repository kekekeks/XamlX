using System;
using System.Collections.Generic;
using XamlX.Transform;
using Xunit;

namespace XamlParserTests
{
    public class IdentifierGeneratorTests
    {
        [Fact]
        public void SequentialGeneratorPreservesReadableNames()
        {
            IXamlIdentifierGenerator generator = new SequentialIdentifierGenerator();

            Assert.Equal("XamlClosure_1", (string)generator.CreateIdentifierFromPrefix("XamlClosure_"));
            Assert.Equal("Property!Field", generator.CreateIdentifierFromPreferredName("Property", 0).AppendSuffix("!Field"));
            Assert.Equal("Property_2!Getter", generator.CreateIdentifierFromPreferredName("Property", 1).AppendSuffix("!Getter"));
        }

        [Fact]
        public void CompactGeneratorProducesReproducibleUniqueNames()
        {
            static string[] Generate()
            {
                IXamlIdentifierGenerator generator = new CompactIdentifierGenerator();
                var names = new List<string>();

                for (var i = 0; i < 100; i++)
                {
                    var identifier = generator.CreateIdentifierFromPreferredName("Property", i);
                    names.Add(identifier.Name);
                    names.Add(identifier.AppendSuffix("!Field"));
                    names.Add(identifier.AppendSuffix("!Getter"));
                    Assert.Equal(names[names.Count - 1], identifier.AppendSuffix("!Getter"));
                }

                return names.ToArray();
            }

            var first = Generate();
            Assert.Equal(first, Generate());
            Assert.Equal(first.Length, new HashSet<string>(first, StringComparer.Ordinal).Count);
        }
    }
}
