using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class CheckingIlEmitter : IXamlILEmitter
    {
        private readonly IXamlILEmitter _inner;
        private Dictionary<IXamlLabel, string> _unmarkedLabels;
        public int StackBalance { get; set; }
        private bool _hasBranches;
        private bool _paused;

        public CheckingIlEmitter(IXamlILEmitter inner)
        {
            _inner = inner;
            _unmarkedLabels = new Dictionary<IXamlLabel, string>();

        }

        public IXamlTypeSystem TypeSystem => _inner.TypeSystem;


        private static readonly Dictionary<StackBehaviour, int> s_balance = new Dictionary<StackBehaviour, int>
        {
            {StackBehaviour.Pop0, 0},
            {StackBehaviour.Pop1, -1},
            {StackBehaviour.Pop1_pop1, -2},
            {StackBehaviour.Popi, -1},
            {StackBehaviour.Popi_pop1, -2},
            {StackBehaviour.Popi_popi, -2},
            {StackBehaviour.Popi_popi8, -2},
            {StackBehaviour.Popi_popi_popi, -3},
            {StackBehaviour.Popi_popr4, -2},
            {StackBehaviour.Popi_popr8, -2},
            {StackBehaviour.Popref, -1},
            {StackBehaviour.Popref_pop1, -2},
            {StackBehaviour.Popref_popi, -2},
            {StackBehaviour.Popref_popi_popi, -3},
            {StackBehaviour.Popref_popi_popi8, -3},
            {StackBehaviour.Popref_popi_popr4, -3},
            {StackBehaviour.Popref_popi_popr8, -3},
            {StackBehaviour.Popref_popi_popref, -3},
            {StackBehaviour.Push0, 0},
            {StackBehaviour.Push1, 1},
            {StackBehaviour.Push1_push1, 2},
            {StackBehaviour.Pushi, 1},
            {StackBehaviour.Pushi8, 1},
            {StackBehaviour.Pushr4, 1},
            {StackBehaviour.Pushr8, 1},
            {StackBehaviour.Pushref, 1},
            {StackBehaviour.Popref_popi_pop1, -3}
        };

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        public void ExplicitStack(int change)
        {
            if (_paused)
                return;
            StackBalance += change;
            (_inner as CheckingIlEmitter)?.ExplicitStack(change);
        }
        
        void Track(OpCode code, IXamlMethod method = null, IXamlConstructor ctor = null)
        {
            if (_paused)
                return;
            if (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch)
                _hasBranches = true;
            
            if (method != null && (code == OpCodes.Call || code == OpCodes.Callvirt))
            {
                StackBalance -= method.Parameters.Count + (method.IsStatic ? 0 : 1);
                if (method.ReturnType.FullName != "System.Void")
                    StackBalance += 1;
            }
            else if (ctor!= null && (code == OpCodes.Call  || code == OpCodes.Newobj))
            {
                StackBalance -= ctor.Parameters.Count;
                if (code == OpCodes.Newobj)
                    // New pushes a value to the stack
                    StackBalance += 1;
                else
                {
                    if (!ctor.IsStatic)
                        // base ctor pops this from the stack
                        StackBalance -= 1;
                }
            }
            else
            {
                void Balance(StackBehaviour op)
                {
                    if (s_balance.TryGetValue(op, out var balance))
                        StackBalance += balance;
                    else
                        throw new Exception("Don't know how to track stack for " + code);
                }
                Balance(code.StackBehaviourPop);
                Balance(code.StackBehaviourPush);
            }
        }
        
        public IXamlILEmitter Emit(OpCode code)
        {
            Track(code);
            _inner.Emit(code);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlField field)
        {
            Track(code);
            _inner.Emit(code, field);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlMethod method)
        {
            Track(code, method);
            _inner.Emit(code, method);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlConstructor ctor)
        {
            Track(code, null, ctor);
            _inner.Emit(code, ctor);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, string arg)
        {
            Track(code);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, int arg)
        {
            Track(code);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, long arg)
        {
            Track(code);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlType type)
        {
            Track(code);
            _inner.Emit(code, type);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, float arg)
        {
            Track(code);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, double arg)
        {
            Track(code);
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
            _unmarkedLabels.Add(label, null);//, Environment.StackTrace);
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
            Track(code);
            _inner.Emit(code, label);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlLocal local)
        {
            Track(code);
            _inner.Emit(code, local);
            return this;
        }

        public void InsertSequencePoint(IFileSource file, int line, int position)
        {
            _inner.InsertSequencePoint(file, line, position);
        }

        public XamlLocalsPool LocalsPool => _inner.LocalsPool;

        public void Check(int expectedBalance)
        {
            if (expectedBalance != StackBalance && !_hasBranches)
                throw new InvalidProgramException($"Unbalanced stack, expected {expectedBalance} got {StackBalance}");
            if (_unmarkedLabels.Count != 0)
                throw new InvalidProgramException("Code block has unmarked labels defined at:\n" +
                                                    string.Join("\n", _unmarkedLabels.Values));
        }
    }
}
