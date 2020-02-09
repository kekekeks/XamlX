using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    class CheckingILEmitter : IXamlILEmitter
    {
        private readonly IXamlILEmitter _inner;

        private Dictionary<IXamlLabel, string> _unmarkedLabels =
            new Dictionary<IXamlLabel, string>();

        private Dictionary<IXamlLabel, Instruction> _labels =
            new Dictionary<IXamlLabel, Instruction>();
        
        private List<IXamlLabel> _labelsToMarkOnNextInstruction = new List<IXamlLabel>();
        private bool _paused;

        public CheckingILEmitter(IXamlILEmitter inner)
        {
            _inner = inner;
        }        
        
        class Instruction
        {
            public int Offset { get; }
            public OpCode Opcode { get; set; }
            public object Operand { get; }
            public int BalanceChange { get; set; }
            public IXamlLabel JumpToLabel { get; set; }
            public Instruction JumpToInstruction { get; set; }
            public int? ExpectedBalance { get; set; }
            public bool IsExplicit { get; set; }

            public Instruction(int offset, OpCode opcode, object operand)
            {
                Offset = offset;
                Opcode = opcode;
                Operand = operand;
                BalanceChange = GetInstructionBalance(opcode, operand);
                JumpToLabel = operand as IXamlLabel;
            }

            public Instruction(int offset, int balanceChange)
            {
                Offset = offset;
                BalanceChange = balanceChange;
                Opcode = OpCodes.Nop;
                IsExplicit = true;
            }

            public override string ToString() =>
                $"{Offset:0000}: {(IsExplicit ? "CALL" : Opcode.ToString())}{(JumpToInstruction != null ? " " + JumpToInstruction.Offset : "")}; Expected {ExpectedBalance} Change {BalanceChange}";
        }
        private List<Instruction> _instructions = new List<Instruction>();


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

        static int GetInstructionBalance(OpCode code, object operand)
        {
            return GetInstructionPopBalance(code, operand) + GetInstructionPushBalance(code, operand);
        }
        
        static int Balance(StackBehaviour op)
        {
            if (s_balance.TryGetValue(op, out var balance))
                return balance;
            else
                throw new Exception("Don't know how to track stack for " + op);
        }
        
        static int GetInstructionPopBalance(OpCode code, object operand)
        {
            var method = operand as IXamlMethod;
            var ctor = operand as IXamlConstructor;
            var stackBalance = 0;
            if (method != null && (code == OpCodes.Call || code == OpCodes.Callvirt))
            {
                stackBalance -= method.Parameters.Count + (method.IsStatic ? 0 : 1);
            }
            else if (ctor!= null && (code == OpCodes.Call  || code == OpCodes.Newobj))
            {
                stackBalance -= ctor.Parameters.Count;
                if(code != OpCodes.Newobj)
                {
                    if (!ctor.IsStatic)
                        // base ctor pops this from the stack
                        stackBalance -= 1;
                }
            }
            else
            {
                stackBalance += Balance(code.StackBehaviourPop);
            }

            return stackBalance;
        }
        
        static int GetInstructionPushBalance(OpCode code, object operand)
        {
            var method = operand as IXamlMethod;
            var ctor = operand as IXamlConstructor;
            var stackBalance = 0;
            if (method != null && (code == OpCodes.Call || code == OpCodes.Callvirt))
            {
                if (method.ReturnType.FullName != "System.Void")
                    stackBalance += 1;
            }
            else if (ctor!= null && (code == OpCodes.Call  || code == OpCodes.Newobj))
            {
                if (code == OpCodes.Newobj)
                    // New pushes a value to the stack
                    stackBalance += 1;
            }
            else
            {
                stackBalance += Balance(code.StackBehaviourPush);
            }

            return stackBalance;
        }
        
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
            _instructions.Add(new Instruction(_instructions.Count, change));
            (_inner as CheckingILEmitter)?.ExplicitStack(change);
        }
        
        void Track(OpCode code, object operand)
        {
            if (_paused)
                return;
            var op = new Instruction(_instructions.Count, code, operand);
            _instructions.Add(op);
            foreach (var l in _labelsToMarkOnNextInstruction)
                _labels[l] = op;
            _labelsToMarkOnNextInstruction.Clear();
        }
        
        public IXamlILEmitter Emit(OpCode code)
        {
            Track(code, null);
            _inner.Emit(code);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlField field)
        {
            Track(code, field);
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
            Track(code, ctor);
            _inner.Emit(code, ctor);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, string arg)
        {
            Track(code, arg);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, int arg)
        {
            Track(code, arg);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, long arg)
        {
            Track(code, arg);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlType type)
        {
            Track(code, type);
            _inner.Emit(code, type);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, float arg)
        {
            Track(code, arg);
            _inner.Emit(code, arg);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, double arg)
        {
            Track(code, arg);
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
            _labelsToMarkOnNextInstruction.Add(label);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlLabel label)
        {
            Track(code, label);
            _inner.Emit(code, label);
            return this;
        }

        public IXamlILEmitter Emit(OpCode code, IXamlLocal local)
        {
            Track(code, local);
            _inner.Emit(code, local);
            return this;
        }

        public void InsertSequencePoint(IFileSource file, int line, int position)
        {
            _inner.InsertSequencePoint(file, line, position);
        }

        public XamlLocalsPool LocalsPool => _inner.LocalsPool;

        string VerifyAndGetBalanceAtExit(int expectedBalance, bool expectReturn)
        {
            if (_instructions.Count == 0)
            {
                if (expectReturn)
                    return "Expected return, but got no instructions";
                if (expectedBalance == 0)
                    return null;
                return $"Expected stack balance {expectedBalance}, but got no instructions";
            }

            var reserve = expectedBalance < 0 ? expectedBalance : 0;
            
            var toInspect = new Stack<int>();
            toInspect.Push(0);
            _instructions[0].ExpectedBalance = 0;

            if (_labelsToMarkOnNextInstruction.Count != 0
                || _instructions.Last().Opcode != OpCodes.Nop
                || _instructions.Last().BalanceChange != 0)
                Track(OpCodes.Nop, null);
            
            foreach(var i in _instructions)
                if (i.JumpToLabel != null)
                    i.JumpToInstruction = _labels[i.JumpToLabel];
            
            int? returnBalance = null;
            while (toInspect.Count > 0)
            {
                var ip = toInspect.Pop();
                var currentBalance = _instructions[ip].ExpectedBalance.Value;
                while (ip < _instructions.Count)
                {
                    var op = _instructions[ip];
                    if (op.ExpectedBalance.HasValue && op.ExpectedBalance != currentBalance)
                        return $"Already have been at instruction offset {ip} ({op.Opcode}) with stack balance {op.ExpectedBalance}, current balance is {currentBalance}";
                    op.ExpectedBalance = currentBalance;

                    if (currentBalance + GetInstructionPopBalance(op.Opcode, op.Operand) < reserve)
                        return $"Stack underflow at {op}";
                    
                    currentBalance += op.BalanceChange;
                    
                    var control = op.Opcode.FlowControl;
                    if (control == FlowControl.Return)
                    {
                        if (!expectReturn)
                            return "Return flow control is not allowed for this emitter";
                        if (returnBalance.HasValue && currentBalance != returnBalance)
                            return 
                                $"Already have a return with different stack balance {returnBalance}, current stack balance is {currentBalance}";
                        returnBalance = currentBalance;
                        if (currentBalance != expectedBalance)
                            return $"Expected balance {expectedBalance}, returned with {currentBalance} at offset {ip}";
                        break;
                    }

                    if (op.JumpToLabel != null)
                    {
                        var jump = _labels[op.JumpToLabel];
                        if (jump.ExpectedBalance.HasValue && jump.ExpectedBalance != currentBalance)
                            return 
                                $"Already have been at instruction offset {jump.Offset} ({jump.Opcode}) with stack balance {jump.ExpectedBalance}, stack balance at jump from {op.Offset} is {currentBalance}";

                        if (jump.ExpectedBalance == null)
                        {
                            jump.ExpectedBalance = currentBalance;
                            toInspect.Push(jump.Offset);
                        }
                    }
                    
                    if (control == FlowControl.Break || control == FlowControl.Throw || control == FlowControl.Branch)
                        break;

                    ip++;
                }
            }
            
            var finalBalance = _instructions.Last().ExpectedBalance;
            if (finalBalance.HasValue && expectReturn)
                return "Expected return, but control reaches the end of IL block";
            if (finalBalance == null && expectReturn)
                return null;
            if (finalBalance != expectedBalance)
                return $"Expected balance {expectedBalance} but got {finalBalance}";
            return null;
        }

        public override string ToString()
        {
            return string.Join("\n", _instructions);
        }


        public string Check(int expectedBalance, bool expectReturn)
        {
            if (_unmarkedLabels.Count != 0)
                return "Code block has unmarked labels defined at:\n" +
                       string.Join("\n", _unmarkedLabels.Values);

            return VerifyAndGetBalanceAtExit(expectedBalance, expectReturn);
        }
    }
}
