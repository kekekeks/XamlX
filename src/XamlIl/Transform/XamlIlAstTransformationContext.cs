using System;
using System.Collections.Generic;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlContextBase
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();  
        public T GetItem<T>() => (T) _items[typeof(T)];

        public T GetOrCreateItem<T>() where T : new()
        {
            if (!_items.TryGetValue(typeof(T), out var rv))
                _items[typeof(T)] = rv = new T();
            return (T)rv;
        }
        
        public bool TryGetItem<T>(out T rv)
        {
            var success = _items.TryGetValue(typeof(T), out var orv);
            rv = (T)orv;
            return success;
        }

        public void SetItem<T>(T item) => _items[typeof(T)] = item;
    }
    
    public class XamlIlAstTransformationContext : XamlIlContextBase
    {
        private List<IXamlIlAstNode> _parentNodes = new List<IXamlIlAstNode>();
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

        public IEnumerable<IXamlIlAstNode> ParentNodes()
        {
            for (var c = _parentNodes.Count - 1; c >= 0; c--)
                yield return _parentNodes[c];
        }

        class Visitor : IXamlIlAstVisitor
        {
            private readonly XamlIlAstTransformationContext _context;
            private readonly IXamlIlAstTransformer _transformer;

            public Visitor(XamlIlAstTransformationContext context, IXamlIlAstTransformer transformer)
            {
                _context = context;
                _transformer = transformer;
            }
            
            public IXamlIlAstNode Visit(IXamlIlAstNode node) => _transformer.Transform(_context, node);

            public void Push(IXamlIlAstNode node) => _context._parentNodes.Add(node);

            public void Pop() => _context._parentNodes.RemoveAt(_context._parentNodes.Count - 1);
        }
        
        public IXamlIlAstNode Visit(IXamlIlAstNode root, IXamlIlAstTransformer transformer)
        {
            root = root.Visit(new Visitor(this, transformer));
            return root;
        }

        public void VisitChildren(IXamlIlAstNode root, IXamlIlAstTransformer transformer)
        {
            root.VisitChildren(new Visitor(this, transformer));
        }
    }
}
