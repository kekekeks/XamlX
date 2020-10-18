using System.Collections.Generic;

namespace XamlParserTests
{
    public class MarkupExtensionContentDictionaryClass
    {
        [Content]
        public Dictionary<string, int> IntDic { get; } = new Dictionary<string, int>();
    }
}
