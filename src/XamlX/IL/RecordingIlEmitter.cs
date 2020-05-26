using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    class RecordingIlEmitter : IXamlILEmitter
    {
        private readonly IXamlILEmitter _inner;
        public List<RecordedInstruction> Instructions { get; } = new List<RecordedInstruction>();

        private readonly Dictionary<IXamlLabel, LabelInfo> _labels
            = new Dictionary<IXamlLabel, LabelInfo>();

        private readonly Dictionary<IXamlLocal, LocalInfo> _locals = new Dictionary<IXamlLocal, LocalInfo>();
        
        public class LocalInfo
        {
            public int Number { get; set; }
            public IXamlType Type { get; set; }

            public override string ToString()
            {
                return "loc_" + Number + " (" + Type + ")";
            }
        }
            
        public class LabelInfo
        {
            public int? Offset { get; set; }
            public override string ToString()
            {
                return Offset?.ToString() ?? "<Unmarked>";
            }
        }
        
        public class RecordedInstruction
        {
            public OpCode OpCode { get; set; }
            public object Operand { get; set; }
        }

        public RecordingIlEmitter(IXamlILEmitter inner)
        {
            _inner = inner;
        }

        public IXamlTypeSystem TypeSystem => _inner.TypeSystem;
        public IXamlILEmitter Emit(OpCode code)
        {
            Record(code, null);
            _inner.Emit(code);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlField field)
        {
            Record(code, field);
            _inner.Emit(code, field);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlMethod method)
        {
            Record(code, method);
            _inner.Emit(code, method);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlConstructor ctor)
        {
            Record(code, ctor);
            _inner.Emit(code, ctor);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, string arg)
        {
            Record(code, arg);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, int arg)
        {
            Record(code, arg);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, long arg)
        {
            Record(code, arg);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlType type)
        {
            Record(code, type);
            _inner.Emit(code, type);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, float arg)
        {
            Record(code, arg);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, double arg)
        {
            Record(code, arg);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlLocal DefineLocal(IXamlType type)
        {
            var rv= _inner.DefineLocal(type);
            _locals[rv] = new LocalInfo {Number = _locals.Count, Type = type};
            return rv;
        }

        public IXamlLabel DefineLabel()
        {
            var label = _inner.DefineLabel();
            _labels[label] = new LabelInfo();
            return label;
        }

        public IXamlILEmitter MarkLabel(IXamlLabel label)
        {
            if (_labels.TryGetValue(label, out var info))
                info.Offset = Instructions.Count;
            _inner.MarkLabel(label);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlLabel label)
        {
            Record(code, label);
            _inner.Emit(code, label);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlLocal local)
        {
            Record(code, local);
            _inner.Emit(code, local);
            return this;
        }

        public void InsertSequencePoint(IFileSource file, int line, int position)
        {
            _inner.InsertSequencePoint(file, line, position);
        }

        public XamlLocalsPool LocalsPool => _inner.LocalsPool;

        void Record(OpCode code, object operand)
        {
            if (operand is IXamlLabel l
                && _labels.TryGetValue(l, out var labelInfo))
                operand = labelInfo;

            if (operand is IXamlLocal loc
                && _locals.TryGetValue(loc, out var localInfo))
                operand = localInfo;

            Instructions.Add(new RecordedInstruction {OpCode = code, Operand = operand});
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for(var c=0; c<Instructions.Count; c++)
            {
                var i = Instructions[c];
                sb.AppendFormat("{0000}", c);
                sb.Append(": ");
                sb.Append(i.OpCode);
                if (i.Operand != null)
                {
                    sb.Append(" ");
                    sb.Append(i.Operand);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
