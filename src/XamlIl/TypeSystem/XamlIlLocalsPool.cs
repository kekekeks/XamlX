using System;
using System.Collections.Generic;

namespace XamlIl.TypeSystem
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlLocalsPool
    {
        private readonly IXamlIlEmitter _emitter;

        private readonly List<(IXamlIlType type, IXamlIlLocal local)> _localsPool =
            new List<(IXamlIlType, IXamlIlLocal)>();
        public sealed class PooledLocal : IDisposable
        {
            public IXamlIlLocal Local { get; private set; }
            private readonly XamlIlLocalsPool _parent;
            private readonly IXamlIlType _type;

            public PooledLocal( XamlIlLocalsPool parent, IXamlIlType type, IXamlIlLocal local)
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
        
        public XamlIlLocalsPool(IXamlIlEmitter emitter)
        {
            _emitter = emitter;
        }
        
        public PooledLocal GetLocal(IXamlIlType type)
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
