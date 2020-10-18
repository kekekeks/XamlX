using System.Collections.Generic;

namespace XamlParserTests
{
    public class MarkupExtensionTestsClass
    {
        public int IntProperty { get; set; }

        public double DoubleProperty { get; set; }

        public int? NullableIntProperty { get; set; }

        public string StringProperty { get; set; }

        public object ObjectProperty { get; set; }

        [Content]
        public List<int> IntList { get; } = new List<int>();

        public List<int> IntList2 { get; } = new List<int>();
    }
}
