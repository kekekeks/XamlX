using System.Collections.Generic;

namespace XamlIl.Ast
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlDocument
    {
        public IXamlIlAstNode Root { get; set; }
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();
    }
}
