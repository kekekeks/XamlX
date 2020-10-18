using System;
using Xunit;

namespace XamlParserTests
{
    public struct ConvertersTestValueType
    {
        public string Value { get; set; }

        public override string ToString() => Value;

        public static ConvertersTestValueType Parse(string s, IFormatProvider prov)
        {
            Assert.NotNull(prov);
            return new ConvertersTestValueType() { Value = s };
        }
    }
}
