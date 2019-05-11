using System;
using System.Collections.Generic;

namespace XamlX.TypeSystem
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXLocalsPool
    {
        private readonly IXamlXEmitter _emitter;

        private readonly List<(IXamlXType type, IXamlXLocal local)> _localsPool =
            new List<(IXamlXType, IXamlXLocal)>();
        public sealed class PooledLocal : IDisposable
        {
            public IXamlXLocal Local { get; private set; }
            private readonly XamlXLocalsPool _parent;
            private readonly IXamlXType _type;

            public PooledLocal( XamlXLocalsPool parent, IXamlXType type, IXamlXLocal local)
            {
                Local = local;
                _parent = parent;
                _type = type;
            }

            public void Dispose()
            {
                if (Local == null)
                    return;
                _parent._localsPool.Add((_type, Local));
                Local = null;
            }
        }
        
        public XamlXLocalsPool(IXamlXEmitter emitter)
        {
            _emitter = emitter;
        }
        
        public PooledLocal GetLocal(IXamlXType type)
        {
            for (var c = 0; c < _localsPool.Count; c++)
            {
                if (_localsPool[c].type.Equals(type))
                {
                    var rv = new PooledLocal(this, type, _localsPool[c].local);
                    _localsPool.RemoveAt(c);
                    return rv;
                }
            }

            return new PooledLocal(this, type, _emitter.DefineLocal(type));

        }
    }
}
