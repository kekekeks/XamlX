using System;
using System.Collections.Generic;
using System.IO;
using XamlX.Ast;

namespace XamlX.Parsers
{
#if !XAMLX_INTERNAL
    public
#endif
    static class XamlParser
    {
        static XamlParser()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("XAML_USE_LEGACY_PARSER")))
            {
                UseLegacyParser = true;
            }
        }

        public static bool UseLegacyParser = false;

        public static XamlDocument Parse(string s, Dictionary<string, string> compatibilityMappings = null)
        {
            if (!UseLegacyParser)
            {
                return GuiLabsXamlParser.Parse(s, compatibilityMappings);
            }
            else
            {
                return XDocumentXamlParser.Parse(s, compatibilityMappings);
            }
            
        }
        public static XamlDocument Parse(TextReader reader, Dictionary<string, string> compatibilityMappings = null)
        {
            if (!UseLegacyParser)
            {
                return GuiLabsXamlParser.Parse(reader, compatibilityMappings);
            }
            else
            {
                return XDocumentXamlParser.Parse(reader, compatibilityMappings);
            }
        }
    }
}
