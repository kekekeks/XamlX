using System;
using System.Collections;
using System.Collections.Generic;
using XamlX.TypeSystem;

namespace XamlX
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlAssemblyList<T> : IReadOnlyList<T>
        where T : IXamlAssembly
    {
        private const int MaxArrayLength = 0X7FEFFFFF;
        private const int InitCapacity = 64;

        private T[] _items = new T[InitCapacity];

        public T this[int index] => _items[index];

        public int Count { get; private set; }

        public void Add(T item)
        {
            EnsureCapacity();

            _items[Count] = item;
            ++Count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; ++i)
                yield return _items[i];
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            this.GetEnumerator();

        private void EnsureCapacity()
        {
            if ((uint)Count < (uint)_items.Length)
                return;

            var min = Count + 1;
            if ((uint)min <= (uint)_items.Length)
                return;

            var capacity = FindNewCapacity(_items.Length, min);
            if (capacity == _items.Length)
                return;

            var newAssemblies = new T[capacity];
            Array.Copy(_items, newAssemblies, _items.Length);
            _items = newAssemblies;
        }

        private static int FindNewCapacity(int currentCapacity, int min)
        {
            var newCapacity = currentCapacity * 2;

            // From  dotnet / runtime / List
            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newCapacity > MaxArrayLength)
                newCapacity = MaxArrayLength;

            if (newCapacity < min)
                newCapacity = min;

            return newCapacity;
        }
    }
}
