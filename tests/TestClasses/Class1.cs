using System;

namespace XamlParserTests
{
    public class SimpleClass
    {
        public string Test { get; set; }
        public string Test2 { get; set; }
        [Content]
        public List<SimpleSubClass> Children { get; set; } = new List<SimpleSubClass>();
    }

    public class SimpleSubClass
    {
        public string Test { get; set; }
    }

}