using System;
using System.Collections.Generic;
using System.Xml;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXContextBase
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();  
        private List<IXamlXAstNode> _parentNodes = new List<IXamlXAstNode>();
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
        
        
        public IEnumerable<IXamlXAstNode> ParentNodes()
        {
            for (var c = _parentNodes.Count - 1; c >= 0; c--)
                yield return _parentNodes[c];
        }

        protected void PushParent(IXamlXAstNode node) => _parentNodes.Add(node);

        protected IXamlXAstNode PopParent()
        {
            var rv = _parentNodes[_parentNodes.Count - 1];
            _parentNodes.RemoveAt(_parentNodes.Count - 1);
            return rv;
        }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXAstTransformationContext : XamlXContextBase
    {
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

        class Visitor : IXamlXAstVisitor
        {
            private readonly XamlXAstTransformationContext _context;
            private readonly IXamlXAstTransformer _transformer;

            public Visitor(XamlXAstTransformationContext context, IXamlXAstTransformer transformer)
            {
                _context = context;
                _transformer = transformer;
            }
            
            public IXamlXAstNode Visit(IXamlXAstNode node)
            {
                #if XAMLIL_DEBUG
                return _transformer.Transform(_context, node);
                #else
                try
                {
                    return _transformer.Transform(_context, node);
                }
                catch (Exception e) when (!(e is XmlException))
                {
                    throw new XamlXParseException(
                        "Internal compiler error while transforming node " + node + ":\n" + e, node);
                }
                #endif
            }

            public void Push(IXamlXAstNode node) => _context.PushParent(node);

            public void Pop() => _context.PopParent();
        }
        
        public IXamlXAstNode Visit(IXamlXAstNode root, IXamlXAstTransformer transformer)
        {
            root = root.Visit(new Visitor(this, transformer));
            return root;
        }

        public void VisitChildren(IXamlXAstNode root, IXamlXAstTransformer transformer)
        {
            root.VisitChildren(new Visitor(this, transformer));
        }
    }
}
