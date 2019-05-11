using System.Collections.Generic;

namespace XamlX.Ast
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXDocument
    {
        public IXamlXAstNode Root { get; set; }
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();
    }
}
