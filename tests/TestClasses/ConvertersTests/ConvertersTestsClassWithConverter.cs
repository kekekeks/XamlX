using System.ComponentModel;

namespace XamlParserTests
{
    [TypeConverter(typeof(TestConverter))]
    public class ConvertersTestsClassWithConverter
    {
        public string Value { get; set; }

        public override string ToString() => Value;
    }
}
