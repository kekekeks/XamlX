using System;
using System.Xml;
using XamlX.Transform;
using Xunit;

namespace XamlParserTests
{
    public class WhitespaceNormalizationTests
    {
        [Theory]
        [InlineData("", false, false, "")]
        public void TestStringNormalization(string input, bool trimStart, bool trimEnd, string expectedResult)
        {
            var node = WhitespaceNormalization.NormalizeWhitespace(input, trimStart, trimEnd);

        }
    }
}