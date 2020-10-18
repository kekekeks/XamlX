using System.Collections.Generic;

namespace XamlParserTests
{
    public class SimpleClassWithDictionaryContent
    {
        public string Test { get; set; }

        [Content]
        public Dictionary<object, object> Children { get; set; } = new Dictionary<object, object>();

        public Dictionary<object, object> NonContentChildren { get; set; } = new Dictionary<object, object>();
    }
}
