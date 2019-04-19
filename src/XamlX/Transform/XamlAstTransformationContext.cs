using System;
using System.Collections.Generic;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlContextBase
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();  
        public T GetItem<T>() => (T) _items[typeof(T)];

        public T GetOrCreateItem<T>() where T : new()
        {
            if (!_items.TryGetValue(typeof(T), out var rv))
                _items[typeof(T)] = rv = new T();
            return (T)rv;
        }

        public void SetItem<T>(T item) => _items[typeof(T)] = item;
    }
    
    public class XamlAstTransformationContext : XamlContextBase
    {
        private List<IXamlAstNode> _parentNodes = new List<IXamlAstNode>();
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();      
        public XamlTransformerConfiguration Configuration { get; }
        public IXamlAstValueNode RootObject { get; set; }
        public bool StrictMode { get; }

        public IXamlAstNode Error(IXamlAstNode node, Exception e)
        {
            if (StrictMode)
                throw e;
            return node;
        }

        public IXamlAstNode ParseError(string message, IXamlAstNode node) =>
            Error(node, new XamlParseException(message, node));
        
        public IXamlAstNode ParseError(string message, IXamlAstNode offender, IXamlAstNode ret) =>
            Error(ret, new XamlParseException(message, offender));

        public XamlAstTransformationContext(XamlTransformerConfiguration configuration,
            Dictionary<string, string> namespaceAliases, bool strictMode = true)
        {
            Configuration = configuration;
            NamespaceAliases = namespaceAliases;
            StrictMode = strictMode;
        }

        public IEnumerable<IXamlAstNode> ParentNodes()
        {
            for (var c = _parentNodes.Count - 1; c >= 0; c--)
                yield return _parentNodes[c];
        }

        class Visitor : IXamlAstVisitor
        {
            private readonly XamlAstTransformationContext _context;
            private readonly IXamlAstTransformer _transformer;

            public Visitor(XamlAstTransformationContext context, IXamlAstTransformer transformer)
            {
                _context = context;
                _transformer = transformer;
            }
            
            public IXamlAstNode Visit(IXamlAstNode node) => _transformer.Transform(_context, node);

            public void Push(IXamlAstNode node) => _context._parentNodes.Add(node);

            public void Pop() => _context._parentNodes.RemoveAt(_context._parentNodes.Count - 1);
        }
        
        public IXamlAstNode Visit(IXamlAstNode root, IXamlAstTransformer transformer)
        {
            root = root.Visit(new Visitor(this, transformer));
            return root;
        }

        public void VisitChildren(IXamlAstNode root, IXamlAstTransformer transformer)
        {
            root.VisitChildren(new Visitor(this, transformer));
        }
    }
}
