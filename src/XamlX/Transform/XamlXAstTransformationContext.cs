using System;
using System.Collections.Generic;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlXAstTransformationContext
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();      
        public XamlXTransformerConfiguration Configuration { get; }
        public IXamlXAstValueNode RootObject { get; set; }
        public bool StrictMode { get; }

        public IXamlXAstNode Error(IXamlXAstNode node, Exception e)
        {
            if (StrictMode)
                throw e;
            return node;
        }

        public IXamlXAstNode ParseError(string message, IXamlXAstNode node) =>
            Error(node, new XamlXParseException(message, node));
        
        public IXamlXAstNode ParseError(string message, IXamlXAstNode offender, IXamlXAstNode ret) =>
            Error(ret, new XamlXParseException(message, offender));

        public XamlXAstTransformationContext(XamlXTransformerConfiguration configuration,
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