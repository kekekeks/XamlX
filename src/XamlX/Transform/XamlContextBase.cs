using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using XamlX.Ast;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlContextBase
    {
        private readonly Dictionary<Type, object> _items = new();
        private readonly List<IXamlAstNode> _parentNodes = [];
        
        public T GetItem<T>() where T : notnull => (T)_items[typeof(T)];

        public T GetOrCreateItem<T>() where T : notnull, new()
        {
            if (!_items.TryGetValue(typeof(T), out var rv))
                _items[typeof(T)] = rv = new T();
            return (T)rv!;
        }

        public bool TryGetItem<T>([NotNullWhen(true)] out T? rv) where T : notnull
        {
            var success = _items.TryGetValue(typeof(T), out var orv);
            rv = (T?)orv;
            return success;
        }

        public void SetItem<T>(T item) where T : notnull => _items[typeof(T)] = item;

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
}
