using System;
using System.Collections.Generic;
using System.Xml;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlContextBase
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();  
        private List<IXamlAstNode> _parentNodes = new List<IXamlAstNode>();
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
        
        
        public IEnumerable<IXamlAstNode> ParentNodes()
        {
            for (var c = _parentNodes.Count - 1; c >= 0; c--)
                yield return _parentNodes[c];
        }

        protected void PushParent(IXamlAstNode node) => _parentNodes.Add(node);

        protected IXamlAstNode PopParent()
        {
            var rv = _parentNodes[_parentNodes.Count - 1];
            _parentNodes.RemoveAt(_parentNodes.Count - 1);
            return rv;
        }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstTransformationContext : XamlContextBase
    {
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

        class Visitor : IXamlAstVisitor
        {
            private readonly XamlAstTransformationContext _context;
            private readonly IXamlAstTransformer _transformer;

            public Visitor(XamlAstTransformationContext context, IXamlAstTransformer transformer)
            {
                _context = context;
                _transformer = transformer;
            }
            
            public IXamlAstNode Visit(IXamlAstNode node)
            {
                #if Xaml_DEBUG
                return _transformer.Transform(_context, node);
                #else
                try
                {
                    return _transformer.Transform(_context, node);
                }
                catch (Exception e) when (!(e is XmlException))
                {
                    throw new XamlParseException(
                        "Internal compiler error while transforming node " + node + ":\n" + e, node);
                }
                #endif
            }

            public void Push(IXamlAstNode node) => _context.PushParent(node);

            public void Pop() => _context.PopParent();
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
