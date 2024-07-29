using System.Collections.Generic;
using XamlX.Ast;
using XamlX.Parsers;

namespace XamlX
{
    public static class XamlParser
    {
        public static XamlDocument Parse(string s, Dictionary<string, string>? compatibilityMappings = null)
        {
#if GUILABS
            return GuiLabsXamlParser.Parse(s, compatibilityMappings);
#else
            return XDocumentXamlParser.Parse(s, compatibilityMappings);
#endif
        }
    }
}
