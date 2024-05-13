using System;
using System.Collections.Generic;

namespace XamlX.Ast
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlDocument
    {
        private IXamlAstNode? _root;

        public IXamlAstNode Root
        {
            get => _root ?? throw new InvalidOperationException($"{nameof(Root)} hasn't been set");
            set => _root = value;
        }

        public string? Document { get; set; }
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();
    }
}
