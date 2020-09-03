using System;
using System.Collections.Generic;
using System.Text;
using XamlX.Parsers;
using Xunit;

namespace XamlParserTests
{
    public class UtilityTests
    {
        [Fact]
        public void LocalNameOnly()
        {
            Assert.Equal(("", "test"), XmlNamespaces.GetPrefixFromName("test"));
        }

        [Theory]
        [InlineData("xmlns", "test", "xmlns:test")]
        public void NameWithNamespace(string ns, string localName, string input)
        {
            Assert.Equal((ns, localName), XmlNamespaces.GetPrefixFromName(input));
        }

        [Theory]
        [InlineData("", "", "")]
        [InlineData("", "", ":")]
        [InlineData("", "test", ":test")]
        [InlineData("xmlns", "", "xmlns:")]
        public void EmptyData(string ns, string localName, string input)
        {
            Assert.Equal((ns, localName), XmlNamespaces.GetPrefixFromName(input));
        }
    }
}
