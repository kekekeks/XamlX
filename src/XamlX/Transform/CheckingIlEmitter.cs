using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class CheckingIlEmitter : IXamlILEmitter, IDisposable
    {
        private readonly IXamlILEmitter _inner;
        private Dictionary<IXamlLabel, string> _unmarkedLabels;

        public CheckingIlEmitter(IXamlILEmitter inner)
        {
            _inner = inner;
            _unmarkedLabels = new Dictionary<IXamlLabel, string>();

        }

        public IXamlTypeSystem TypeSystem => _inner.TypeSystem;

        public IXamlILEmitter Emit(OpCode code)
        {
            _inner.Emit(code);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlField field)
        {
            _inner.Emit(code, field);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlMethod method)
        {
            _inner.Emit(code, method);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlConstructor ctor)
        {
            _inner.Emit(code, ctor);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, string arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, int arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, long arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlType type)
        {
            _inner.Emit(code, type);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, float arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, double arg)
        {
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlLocal DefineLocal(IXamlType type)
        {
            return _inner.DefineLocal(type);
        }

        public IXamlLabel DefineLabel()
        {
            var label = _inner.DefineLabel();
            _unmarkedLabels.Add(label, Environment.StackTrace);
            return label;
        }

        public IXamlILEmitter MarkLabel(IXamlLabel label)
        {
            if (!_unmarkedLabels.Remove(label))
                throw new InvalidOperationException("Attempt to mark undeclared label");
            _inner.MarkLabel(label);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlLabel label)
        {
            _inner.Emit(code, label);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlLocal local)
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
