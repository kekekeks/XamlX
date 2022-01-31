using System.Collections;
using System.Collections.Generic;

namespace XamlParserTests
{
    public class Control
    {
        public string StrProp { get; set; }

        public bool BoolProp { get; set; }
    }

    public class ContentControl : Control
    {
        [Content]
        public object Content { get; set; }
    }

    public class MixedContentControl
    {
        [Content]
        public List<object> Content { get; } = new List<object>();
    }

    public class MixedEnumerableContentControl
    {
        [Content] public IEnumerable Items { get; set; } = new List<object>();
    }

    // This control uses a collection as it's content property which declares
    // the WhitespaceSignificantCollection property.
    public class WhitespaceOptInControl
    {
        [Content]
        public WhitespaceOptInCollection Content { get; } = new WhitespaceOptInCollection();
    }

    [WhitespaceSignificantCollection]
    public class WhitespaceOptInCollection : List<object>
    {
    }

    [TrimSurroundingWhitespace]
    public class TrimControl
    {
    }
}
