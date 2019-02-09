using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlXEmitContext
    {
        private readonly List<object> _emitters;

        private readonly Dictionary<XamlXAstCompilerLocalNode, IXamlXLocal>
            _locals = new Dictionary<XamlXAstCompilerLocalNode, IXamlXLocal>();
        
        public XamlXTransformerConfiguration Configuration { get; }
        public XamlXContext RuntimeContext { get; }
        public IXamlXLocal ContextLocal { get; }
        public IXamlXEmitter Emitter { get; }

        private List<(IXamlXType type, IXamlXLocal local)> _localsPool =
            new List<(IXamlXType, IXamlXLocal)>();

        public sealed class PooledLocal : IDisposable
        {
            public IXamlXLocal Local { get; private set; }
            private readonly XamlXEmitContext _parent;
            private readonly IXamlXType _type;

            public PooledLocal(XamlXEmitContext parent, IXamlXType type, IXamlXLocal local)
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

        public XamlXEmitContext(IXamlXEmitter emitter, XamlXTransformerConfiguration configuration,
            XamlXContext runtimeContext, IXamlXLocal contextLocal,
            IEnumerable<object> emitters)
        {
            Emitter = emitter;
            _emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
        }

        public void StLocal(XamlXAstCompilerLocalNode node)
        {
            if (!_locals.TryGetValue(node, out var local))
                _locals[node] = local = Emitter.DefineLocal(node.Type);

            Emitter.Emit(OpCodes.Stloc, local);
        }

        public void LdLocal(XamlXAstCompilerLocalNode node)
        {
            if (_locals.TryGetValue(node, out var local))
                Emitter.Emit(OpCodes.Ldloc, local);
            else
                throw new XamlXLoadException("Attempt to read uninitialized local variable", node);
        }

        public PooledLocal GetLocal(IXamlXType type)
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
        
        public XamlXNodeEmitResult Emit(IXamlXAstNode value, IXamlXEmitter codeGen, IXamlXType expectedType)
        {
            var res = EmitCore(value, codeGen);
            var returnedType = res.ReturnType;

            if (returnedType != null || expectedType != null)
            {

                if (returnedType != null && expectedType == null)
                    throw new XamlXLoadException(
                        $"Emit of node {value} resulted in {returnedType.GetFqn()} while caller expected void", value);

                if (expectedType != null && returnedType == null)
                    throw new XamlXLoadException(
                        $"Emit of node {value} resulted in void while caller expected {expectedType.GetFqn()}", value);

                if (!returnedType.Equals(expectedType))
                {
                    PooledLocal local = null;
                    // ReSharper disable once ExpressionIsAlwaysNull
                    // Value is assigned inside the closure in certain conditions
                    using (local)
                        TypeSystemHelpers.EmitConvert(value, returnedType, expectedType, ldaddr =>
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

        private XamlXNodeEmitResult EmitCore(IXamlXAstNode value, IXamlXEmitter codeGen)
        {
            XamlXNodeEmitResult res = null;
            foreach (var e in _emitters)
            {
                if (e is IXamlXAstNodeEmitter ve)
                {
                    res = ve.Emit(value, this, codeGen);
                    if (res != null)
                        return res;
                }
            }

            if (value is IXamlXAstEmitableNode en)
                return en.Emit(this, codeGen);
            else
                throw new XamlXLoadException("Unable to find emitter for node type: " + value.GetType().FullName,
                    value);
        }
    }
}