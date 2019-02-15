using System;
using System.Collections.Generic;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlAstTransformationContext
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();      
        public XamlIlTransformerConfiguration Configuration { get; }
        public IXamlIlAstValueNode RootObject { get; set; }
        public bool StrictMode { get; }

        public IXamlIlAstNode Error(IXamlIlAstNode node, Exception e)
        {
            if (StrictMode)
                throw e;
            return node;
        }

        public IXamlIlAstNode ParseError(string message, IXamlIlAstNode node) =>
            Error(node, new XamlIlParseException(message, node));
        
        public IXamlIlAstNode ParseError(string message, IXamlIlAstNode offender, IXamlIlAstNode ret) =>
            Error(ret, new XamlIlParseException(message, offender));

        public XamlIlAstTransformationContext(XamlIlTransformerConfiguration configuration,
            Dictionary<string, string> namespaceAliases, bool strictMode = true)
        {
            Configuration = configuration;
            NamespaceAliases = namespaceAliases;
            StrictMode = strictMode;
        }

        public T GetItem<T>() => (T) _items[typeof(T)];
        public void SetItem<T>(T item) => _items[typeof(T)] = item;       
    }
}