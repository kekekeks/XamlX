using System.Collections.Generic;

namespace XamlParserTests
{
    public class EnumerableContentClass
    {
        [Content]
        public IEnumerable<SimpleSubClass> Children { get; set; } = new List<SimpleSubClass>();
    }
}
