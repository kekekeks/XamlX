using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlEmitContext
    {
        private readonly List<object> _emitters;

        private readonly Dictionary<XamlIlAstCompilerLocalNode, (IXamlIlLocal local, IXamlIlEmitter codegen)>
            _locals = new Dictionary<XamlIlAstCompilerLocalNode, (IXamlIlLocal local, IXamlIlEmitter codegen)>();
        public XamlIlTransformerConfiguration Configuration { get; }
        public XamlIlContext RuntimeContext { get; }
        public IXamlIlLocal ContextLocal { get; }
        private List<(IXamlIlType type, IXamlIlEmitter codeGen, IXamlIlLocal local)> _localsPool = 
            new List<(IXamlIlType, IXamlIlEmitter, IXamlIlLocal)>();

        public sealed class PooledLocal : IDisposable
        {
            public IXamlIlLocal Local { get; private set; }
            private readonly XamlIlEmitContext _parent;
            private readonly IXamlIlType _type;
            private readonly IXamlIlEmitter _codeGen;

            public PooledLocal(XamlIlEmitContext parent,  IXamlIlType type, IXamlIlEmitter codeGen, IXamlIlLocal local)
            {
                Local = local;
                _parent = parent;
                _type = type;
                _codeGen = codeGen;
            }

            public void Dispose()
            {
                if (Local == null)
                    return;
                _parent._localsPool.Add((_type, _codeGen, Local));
                Local = null;
            }
        }

        public XamlIlEmitContext(XamlIlTransformerConfiguration configuration,
            XamlIlContext runtimeContext, IXamlIlLocal contextLocal,
            IEnumerable<object> emitters)
        {
            _emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
        }

        public void StLocal(XamlIlAstCompilerLocalNode node,  IXamlIlEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlIlLoadException("Local node is assigned to a different codegen", node);
            }
            else
                _locals[node] = local = (codeGen.DefineLocal(node.Type), codeGen);

            codeGen.Emit(OpCodes.Stloc, local.local);
        }

        public void LdLocal(XamlIlAstCompilerLocalNode node, IXamlIlEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlIlLoadException("Local node is assigned to a different codegen", node);
                codeGen.Emit(OpCodes.Ldloc, local.local);
            }
            else
                throw new XamlIlLoadException("Attempt to read uninitialized local variable", node);
        }

        public PooledLocal GetLocal(IXamlIlEmitter codeGen, IXamlIlType type)
        {
            for (var c = 0; c < _localsPool.Count; c++)
            {
                if (_localsPool[c].type.Equals(type))
                {
                    var rv = new PooledLocal(this, type, codeGen, _localsPool[c].local);
                    _localsPool.RemoveAt(c);
                    return rv;
                }
            }

            return new PooledLocal(this, type, codeGen, codeGen.DefineLocal(type));

        }
        
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode value, IXamlIlEmitter codeGen, IXamlIlType expectedType)
        {
            var res = EmitCore(value, codeGen);
            var returnedType = res.ReturnType;

            if (returnedType != null || expectedType != null)
            {

                if (returnedType != null && expectedType == null)
                    throw new XamlIlLoadException(
                        $"Emit of node {value} resulted in {returnedType.GetFqn()} while caller expected void", value);

                if (expectedType != null && returnedType == null)
                    throw new XamlIlLoadException(
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
                                local = GetLocal(codeGen, returnedType);
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

        private XamlIlNodeEmitResult EmitCore(IXamlIlAstNode value, IXamlIlEmitter codeGen)
        {
            XamlIlNodeEmitResult res = null;
            foreach (var e in _emitters)
            {
                if (e is IXamlIlAstNodeEmitter ve)
                {
                    res = ve.Emit(value, this, codeGen);
                    if (res != null)
                        return res;
                }
            }

            if (value is IXamlIlAstEmitableNode en)
                return en.Emit(this, codeGen);
            else
                throw new XamlIlLoadException("Unable to find emitter for node type: " + value.GetType().FullName,
                    value);
        }
    }
}