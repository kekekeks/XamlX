using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using XamlX.IL;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using SreOpCode = System.Reflection.Emit.OpCode;
using SreOpCodes = System.Reflection.Emit.OpCodes;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        public class CecilEmitter : IXamlILEmitter
        {
            TypeReference Import(TypeReference r)
            {
                var rv = M.ImportReference(r);
                if (r is GenericInstanceType gi)
                {
                    for (var c = 0; c < gi.GenericArguments.Count; c++)
                        gi.GenericArguments[c] = Import(gi.GenericArguments[c]);
                }

                return rv;
            }

            FieldReference Import(FieldReference f)
            {
                var rv = M.ImportReference(f);
                rv.FieldType = Import(rv.FieldType);
                return rv;
            }
            
            private static Dictionary<SreOpCode, OpCode> Dic = new Dictionary<SreOpCode, OpCode>();
            private List<CecilLabel> _markedLabels = new List<CecilLabel>();
            static CecilEmitter()
            {

                foreach (var sreField in typeof(SreOpCodes)
                    .GetFields(BindingFlags.Static | BindingFlags.Public)
                    .Where(f=>f.FieldType == typeof(SreOpCode)))

                {
                    var sre = (SreOpCode) sreField.GetValue(null);
                    var cecilField = typeof(OpCodes).GetField(sreField.Name);
                    if(cecilField == null)
                        continue;
                    var cecil = (OpCode)cecilField.GetValue(null);
                    Dic[sre] = cecil;
                }       
            }
            
            
            private readonly MethodBody _body;
            private readonly MethodDefinition _method;
            private CecilDebugPoint _pendingDebugPoint;
            private CecilDebugPoint _lastDebugPoint;
            public CecilEmitter(IXamlTypeSystem typeSystem, MethodDefinition method)
            {
                _method = method;
                _body = method.Body;
                TypeSystem = typeSystem;
                LocalsPool = new XamlLocalsPool(t => this.DefineLocal(t));
            }


            public IXamlTypeSystem TypeSystem { get; }

            IXamlILEmitter Emit(Instruction i)
            {
                _body.Instructions.Add(i);
                if (_pendingDebugPoint != null)
                {
                    _pendingDebugPoint.Create(i);
                    _pendingDebugPoint = null;
                }
                foreach (var ml in _markedLabels)
                    ml.Mark(i);
                _markedLabels.Clear();
                return this;
            }

            ParameterDefinition GetParameter(int arg)
            {
                if (_body.Method.HasThis)
                {
                    if (arg == 0)
                        return _body.ThisParameter;
                    arg--;
                }

                return _body.Method.Parameters[arg];
            }

            private Instruction CreateI(OpCode code, int arg)
            {
                if (code.OperandType == OperandType.ShortInlineArg || code.OperandType == OperandType.InlineArg)
                    return Instruction.Create(code, GetParameter(arg));
                if (code.OperandType == OperandType.InlineVar || code.OperandType == OperandType.ShortInlineVar)
                    return Instruction.Create(code, _body.Variables[arg]);
                return Instruction.Create(code, arg);
            }

            private ModuleDefinition M => _body.Method.Module;

            public IXamlILEmitter Emit(SreOpCode code)
                => Emit(Instruction.Create(Dic[code]));

            public IXamlILEmitter Emit(SreOpCode code, IXamlField field)
            {
                return Emit(Instruction.Create(Dic[code], Import(((CecilField) field).Field)));
            }

            public IXamlILEmitter Emit(SreOpCode code, IXamlMethod method)
                => Emit(Instruction.Create(Dic[code], M.ImportReference(((CecilMethod) method).IlReference)));

            public IXamlILEmitter Emit(SreOpCode code, IXamlConstructor ctor)
                => Emit(Instruction.Create(Dic[code], M.ImportReference(((CecilConstructor) ctor).IlReference)));

            public IXamlILEmitter Emit(SreOpCode code, string arg)
                => Emit(Instruction.Create(Dic[code], arg));

            public IXamlILEmitter Emit(SreOpCode code, int arg)
                => Emit(CreateI(Dic[code], arg));

            public IXamlILEmitter Emit(SreOpCode code, long arg)
                => Emit(Instruction.Create(Dic[code], arg));

            public IXamlILEmitter Emit(SreOpCode code, IXamlType type)
                => Emit(Instruction.Create(Dic[code], Import(((ITypeReference) type).Reference)));

            public IXamlILEmitter Emit(SreOpCode code, float arg)
                => Emit(Instruction.Create(Dic[code], arg));

            public IXamlILEmitter Emit(SreOpCode code, double arg)
                => Emit(Instruction.Create(Dic[code], arg));


            class CecilLocal : IXamlLocal
            {
                public VariableDefinition Variable { get; set; }
            }

            class CecilLabel : IXamlLabel
            {
                private List<Instruction> _pendingConsumers;
                private Instruction _target;
                public Instruction CreateInstruction(OpCode opcode)
                {
                    if (_target != null)
                        return Instruction.Create(opcode, _target);

                    var rv = Instruction.Create(opcode, Instruction.Create(OpCodes.Nop));
                    if (_pendingConsumers == null)
                        _pendingConsumers = new List<Instruction>();
                    _pendingConsumers.Add(rv);
                    return rv;
                }

                public void Mark(Instruction i)
                {
                    if (_target != null)
                        throw new InvalidOperationException();
                    _target = i;
                    if(_pendingConsumers != null)
                        foreach (var c in _pendingConsumers)
                            c.Operand = i;
                    _pendingConsumers = null;
                }
            }

            public IXamlLocal DefineLocal(IXamlType type)
            {
                var r = Import(((ITypeReference) type).Reference);
                var def = new VariableDefinition(r);
                _body.Variables.Add(def);
                return new CecilLocal {Variable = def};
            }

            public IXamlLabel DefineLabel() => new CecilLabel();

            public IXamlILEmitter MarkLabel(IXamlLabel label)
            {
                _markedLabels.Add((CecilLabel) label);
                return this;
            }

            public IXamlILEmitter Emit(SreOpCode code, IXamlLabel label)
                => Emit(((CecilLabel)label).CreateInstruction(Dic[code]));

            public IXamlILEmitter Emit(SreOpCode code, IXamlLocal local)
                => Emit(Instruction.Create(Dic[code], ((CecilLocal) local).Variable));

            private static readonly Guid LanguageGuid = new Guid("9a37fc74-96b5-4dbc-8b8a-c4e603735a63");
            private static readonly Guid LanguageVendorGuid = new Guid("3c631bf9-0cbe-4aab-a24a-5e417734441c");
            
            class CecilDebugPoint
            {
                private readonly CecilEmitter _parent;
                public DocumentHelper Document { get; }
                public int Line { get; }
                public int Position { get; }

                public CecilDebugPoint(CecilEmitter parent, DocumentHelper document, int line, int position)
                {
                    _parent = parent;
                    Document = document;
                    Line = line;
                    Position = position;
                }

                public void Create(Instruction instruction)
                {
                    // Step into doesn't work for methods without sequence points in the first instruction
                    if (!_parent._method.DebugInformation.HasSequencePoints
                        && _parent._body.Instructions.Count != 0)
                        instruction = _parent._body.Instructions.First();
                    
                    var dbg = _parent._method.DebugInformation;
                    if (dbg.Scope == null)
                    {
                        dbg.Scope = new ScopeDebugInformation(instruction, instruction)
                        {
                            End = new InstructionOffset(),
                            Import = new ImportDebugInformation()
                        };
                    }

                    var realLine = Line - 1;
                    var endColumn = Position;
                    if (realLine<Document.Lines.Count)
                    {
                        var lineString = Document.Lines[realLine];
                        for (; endColumn < lineString.Length; endColumn++)
                        {
                            var ch = lineString[endColumn];
                            if (ch == ':' || char.IsDigit(ch) || char.IsLetter(ch))
                                continue;
                            break;
                        }
                    }

                    endColumn++;
                    
                    var sp = new SequencePoint(instruction, Document.Document)
                    {
                        StartLine = Line,
                        StartColumn = Position,
                        EndLine = Line,
                        EndColumn = endColumn
                    };
                    dbg.SequencePoints.Add(sp);
                    
                }
            }
            
            static ConditionalWeakTable<AssemblyDefinition, Dictionary<string, DocumentHelper>>
                _documents = new ConditionalWeakTable<AssemblyDefinition, Dictionary<string, DocumentHelper>>();

            class DocumentHelper
            {
                public Document Document { get; }
                public List<string> Lines { get; } = new List<string>();
                public DocumentHelper(IFileSource file)
                {
                    var data = file.FileContents;
                    byte[] hash;
                    using (var sha1 = SHA1.Create())
                        hash = sha1.ComputeHash(data);
                    
                    Document = new Document(file.FilePath)
                    {
                        LanguageGuid = LanguageGuid,
                        LanguageVendorGuid = LanguageVendorGuid,
                        Type = DocumentType.Text,
                        HashAlgorithm = DocumentHashAlgorithm.SHA1,
                        Hash = hash,
                    };
                    var r = new StreamReader(new MemoryStream(data));
                    string l;
                    while ((l = r.ReadLine()) != null)
                        Lines.Add(l);
                }
            }
            
            public void InsertSequencePoint(IFileSource file, int line, int position)
            {
                if (!_documents.TryGetValue(_method.Module.Assembly, out var documents))
                    _documents.Add(_method.Module.Assembly, documents = new Dictionary<string, DocumentHelper>());
                
                if (!documents.TryGetValue(file.FilePath, out var doc))
                {
                    documents[file.FilePath] = doc = new DocumentHelper(file);
                }

                if (_pendingDebugPoint == null)
                {
                    if (_lastDebugPoint == null ||
                        !(_lastDebugPoint.Document == doc && _lastDebugPoint.Line == line &&
                          _lastDebugPoint.Position == position))
                        _pendingDebugPoint = _lastDebugPoint = new CecilDebugPoint(this, doc, line, position);
                }
            }

            public XamlLocalsPool LocalsPool { get; }
        }
    }
}
