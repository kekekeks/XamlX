using System;
using System.Collections.Generic;

namespace XamlX.TypeSystem
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlLocalsPool
    {
        private readonly Func<IXamlType, IXamlLocal> _localFactory;

        private readonly List<(IXamlType type, IXamlLocal local)> _localsPool =
            new List<(IXamlType, IXamlLocal)>();

        public sealed class PooledLocal : IDisposable
        {
            private IXamlLocal? _local;

            public IXamlLocal Local
                => _local ?? throw new ObjectDisposedException(nameof(PooledLocal));

            private readonly XamlLocalsPool _parent;
            private readonly IXamlType _type;

            public PooledLocal(XamlLocalsPool parent, IXamlType type, IXamlLocal local)
            {
                _local = local;
                _parent = parent;
                _type = type;
            }

            public void Dispose()
            {
                if (_local == null)
                    return;
                _parent._localsPool.Add((_type, Local));
                _local = null;
            }
        }
        
        public XamlLocalsPool(Func<IXamlType, IXamlLocal> localFactory)
        {
            _localFactory = localFactory;
        }
        
        public PooledLocal GetLocal(IXamlType type)
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

            return new PooledLocal(this, type, _localFactory(type));

        }
    }
}
