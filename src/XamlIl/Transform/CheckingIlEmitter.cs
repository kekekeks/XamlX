using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class CheckingIlEmitter : IXamlIlEmitter, IDisposable
    {
        private readonly IXamlIlEmitter _inner;
        private Dictionary<IXamlIlLabel, string> _unmarkedLabels;

        public CheckingIlEmitter(IXamlIlEmitter inner)
        {
            _inner = inner;
            _unmarkedLabels = new Dictionary<IXamlIlLabel, string>();

        }

        public IXamlIlTypeSystem TypeSystem => _inner.TypeSystem;

        public IXamlIlEmitter Emit(OpCode code)
        {
            _inner.Emit(code);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, IXamlIlField field)
        {
            _inner.Emit(code, field);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, IXamlIlMethod method)
        {
            _inner.Emit(code, method);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, IXamlIlConstructor ctor)
        {
            _inner.Emit(code, ctor);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, string arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, int arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, long arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, IXamlIlType type)
        {
            _inner.Emit(code, type);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, float arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, double arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlIlLocal DefineLocal(IXamlIlType type)
        {
            return _inner.DefineLocal(type);
        }

        public IXamlIlLabel DefineLabel()
        {
            var label = _inner.DefineLabel();
            _unmarkedLabels.Add(label, Environment.StackTrace);
            return label;
        }

        public IXamlIlEmitter MarkLabel(IXamlIlLabel label)
        {
            if (!_unmarkedLabels.Remove(label))
                throw new InvalidOperationException("Attempt to mark undeclared label");
            _inner.MarkLabel(label);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, IXamlIlLabel label)
        {
            _inner.Emit(code, label);
            return this;
        }

        public IXamlIlEmitter Emit(OpCode code, IXamlIlLocal local)
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
