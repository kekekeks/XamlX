using System;

namespace XamlParserTests
{
    public struct ConvertersTestValueType
    {
        public static event Action<IFormatProvider> ParseEventRequiredAssert;

        public string Value { get; set; }

        public override string ToString() => Value;

        public static ConvertersTestValueType Parse(string s, IFormatProvider prov)
        {
            ParseEventRequiredAssert.Invoke(prov);
            return new ConvertersTestValueType() { Value = s };
        }
    }
}
