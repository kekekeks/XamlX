using System;
using System.Collections.Generic;
using System.Text;
using XamlX.Ast;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlContextBase
    {
        private readonly Dictionary<Type, object> _items = new Dictionary<Type, object>();
        private readonly List<IXamlAstNode> _parentNodes = new List<IXamlAstNode>();
        private int _idCount;
        
        public int GetNextUniqueContextId () => ++_idCount;
        
        public T GetItem<T>() => (T)_items[typeof(T)];

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
}
