using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlEmitContext
    {
        public List<object> Emitters { get; }

        private readonly Dictionary<XamlAstCompilerLocalNode, IXamlLocal>
            _locals = new Dictionary<XamlAstCompilerLocalNode, IXamlLocal>();
        
        public XamlTransformerConfiguration Configuration { get; }
        public XamlContext RuntimeContext { get; }
        public IXamlLocal ContextLocal { get; }
        public Func<string, IXamlType, IXamlTypeBuilder> CreateSubType { get; }
        public IXamlILEmitter Emitter { get; }

        private readonly List<(IXamlType type, IXamlLocal local)> _localsPool =
            new List<(IXamlType, IXamlLocal)>();

        public sealed class PooledLocal : IDisposable
        {
            public IXamlLocal Local { get; private set; }
            private readonly XamlEmitContext _parent;
            private readonly IXamlType _type;

            public PooledLocal(XamlEmitContext parent, IXamlType type, IXamlLocal local)
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

        public XamlEmitContext(IXamlILEmitter emitter, XamlTransformerConfiguration configuration,
            XamlContext runtimeContext, IXamlLocal contextLocal, 
            Func<string, IXamlType, IXamlTypeBuilder> createSubType, IEnumerable<object> emitters)
        {
            Emitter = emitter;
            Emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
            CreateSubType = createSubType;
        }

        public void StLocal(XamlAstCompilerLocalNode node)
        {
            if (!_locals.TryGetValue(node, out var local))
                _locals[node] = local = Emitter.DefineLocal(node.Type);

            Emitter.Emit(OpCodes.Stloc, local);
        }

        public void LdLocal(XamlAstCompilerLocalNode node)
        {
            if (_locals.TryGetValue(node, out var local))
                Emitter.Emit(OpCodes.Ldloc, local);
            else
                throw new XamlLoadException("Attempt to read uninitialized local variable", node);
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

            return new PooledLocal(this, type, Emitter.DefineLocal(type));

        }
        
        public XamlNodeEmitResult Emit(IXamlAstNode value, IXamlILEmitter codeGen, IXamlType expectedType)
        {
            var checkedEmitter = new CheckingIlEmitter(codeGen); 
            var res = EmitCore(value, checkedEmitter);
            checkedEmitter.Check();

            var returnedType = res.ReturnType;

            if (returnedType != null || expectedType != null)
            {

                if (returnedType != null && expectedType == null)
                    throw new XamlLoadException(
                        $"Emit of node {value} resulted in {returnedType.GetFqn()} while caller expected void", value);

                if (expectedType != null && returnedType == null)
                    throw new XamlLoadException(
                        $"Emit of node {value} resulted in void while caller expected {expectedType.GetFqn()}", value);

                if (!returnedType.Equals(expectedType))
                {
                    PooledLocal local = null;
                    // ReSharper disable once ExpressionIsAlwaysNull
                    // Value is assigned inside the closure in certain conditions
                    using (local)
                        TypeSystemHelpers.EmitConvert(this, value, returnedType, expectedType, ldaddr =>
                        {
                            if (ldaddr && returnedType.IsValueType)
                            {
                                // We need to store the value to a temporary variable, since *address*
                                // is required (probably for  method call on the value type)
                                local = GetLocal(returnedType);
                                codeGen
                                    .Stloc(local.Local)
                                    .Ldloca(local.Local);

                            }
                            // Otherwise do nothing, value is already at the top of the stack
                            return codeGen;
                        });
                }

            }

            return res;
        }

        private XamlNodeEmitResult EmitCore(IXamlAstNode value, IXamlILEmitter codeGen)
        {
            XamlNodeEmitResult res = null;
            foreach (var e in Emitters)
            {
                if (e is IXamlAstNodeEmitter ve)
                {
                    res = ve.Emit(value, this, codeGen);
                    if (res != null)
                        return res;
                }
            }

            if (value is IXamlAstEmitableNode en)
                return en.Emit(this, codeGen);
            else
                throw new XamlLoadException("Unable to find emitter for node type: " + value.GetType().FullName,
                    value);
        }
    }
}
