using System;
using System.Collections.Generic;
using XamlX.Ast;
using XamlX.Parsers;

namespace XamlX
{
    public static class XamlParser
    {
        public static bool UseXDocumentParser { get; set; } = false;

        public static XamlDocument Parse(string s, Dictionary<string, string> compatibilityMappings = null)
        {
            if (UseXDocumentParser)
            {
                return XDocumentXamlParser.Parse(s, compatibilityMappings);
            }

            return GuiLabsXamlParser.Parse(s, compatibilityMappings);
        }
    }
}
