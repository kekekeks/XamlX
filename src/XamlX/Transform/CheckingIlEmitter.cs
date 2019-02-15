using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class CheckingIlEmitter : IXamlXEmitter, IDisposable
    {
        private readonly IXamlXEmitter _inner;
        private Dictionary<IXamlXLabel, string> _unmarkedLabels;

        public CheckingIlEmitter(IXamlXEmitter inner)
        {
            _inner = inner;
            _unmarkedLabels = new Dictionary<IXamlXLabel, string>();

        }

        public IXamlXTypeSystem TypeSystem => _inner.TypeSystem;

        public IXamlXEmitter Emit(OpCode code)
        {
            _inner.Emit(code);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, IXamlXField field)
        {
            _inner.Emit(code, field);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, IXamlXMethod method)
        {
            _inner.Emit(code, method);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, IXamlXConstructor ctor)
        {
            _inner.Emit(code, ctor);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, string arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, int arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, long arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, IXamlXType type)
        {
            _inner.Emit(code, type);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, float arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, double arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlXLocal DefineLocal(IXamlXType type)
        {
            return _inner.DefineLocal(type);
        }

        public IXamlXLabel DefineLabel()
        {
            var label = _inner.DefineLabel();
            _unmarkedLabels.Add(label, Environment.StackTrace);
            return label;
        }

        public IXamlXEmitter MarkLabel(IXamlXLabel label)
        {
            if (!_unmarkedLabels.Remove(label))
                throw new InvalidOperationException("Attempt to mark undeclared label");
            _inner.MarkLabel(label);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, IXamlXLabel label)
        {
            _inner.Emit(code, label);
            return this;
        }

        public IXamlXEmitter Emit(OpCode code, IXamlXLocal local)
        {
            _inner.Emit(code, local);
            return this;
        }

        public void Dispose() => Check();
        public void Check()
        {
            if (_unmarkedLabels.Count != 0)
                throw new InvalidOperationException("Code block has unmarked labels defined at:\n" +
                                                    string.Join("\n", _unmarkedLabels.Values));
        }
    }
}
