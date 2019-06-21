using System;
using System.Collections.Generic;
using System.Xml;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlContextBase
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();  
        private List<IXamlIlAstNode> _parentNodes = new List<IXamlIlAstNode>();
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
        
        
        public IEnumerable<IXamlIlAstNode> ParentNodes()
        {
            for (var c = _parentNodes.Count - 1; c >= 0; c--)
                yield return _parentNodes[c];
        }

        protected void PushParent(IXamlIlAstNode node) => _parentNodes.Add(node);

        protected IXamlIlAstNode PopParent()
        {
            var rv = _parentNodes[_parentNodes.Count - 1];
            _parentNodes.RemoveAt(_parentNodes.Count - 1);
            return rv;
        }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlAstTransformationContext : XamlIlContextBase
    {
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

        class Visitor : IXamlIlAstVisitor
        {
            private readonly XamlIlAstTransformationContext _context;
            private readonly IXamlIlAstTransformer _transformer;

            public Visitor(XamlIlAstTransformationContext context, IXamlIlAstTransformer transformer)
            {
                _context = context;
                _transformer = transformer;
            }
            
            public IXamlIlAstNode Visit(IXamlIlAstNode node)
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
                    throw new XamlIlParseException(
                        "Internal compiler error while transforming node " + node + ":\n" + e, node);
                }
                #endif
            }

            public void Push(IXamlIlAstNode node) => _context.PushParent(node);

            public void Pop() => _context.PopParent();
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
