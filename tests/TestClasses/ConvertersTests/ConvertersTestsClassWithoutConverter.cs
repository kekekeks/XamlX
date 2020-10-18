namespace XamlParserTests
{
    public class ConvertersTestsClassWithoutConverter
    {
        public string Value { get; set; }

        public override string ToString() => Value;
    }
}
