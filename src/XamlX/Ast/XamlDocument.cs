using System.Collections.Generic;

namespace XamlX.Ast
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlDocument
    {
        public IXamlAstNode Root { get; set; }
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();
    }
}
