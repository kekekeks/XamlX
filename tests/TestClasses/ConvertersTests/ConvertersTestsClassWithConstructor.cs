using System;

namespace XamlParserTests
{
    public class ConvertersTestsClassWithConstructor
    {
        public int Int;

        public TimeSpan Converted;

        public ConvertersTestsClassWithConstructor(int i, TimeSpan converted)
        {
            Int = i;
            Converted = converted;
        }
    }
}
