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

        private readonly Dictionary<XamlXAstCompilerLocalNode, (IXamlXLocal local, IXamlXEmitter codegen)>
            _locals = new Dictionary<XamlXAstCompilerLocalNode, (IXamlXLocal local, IXamlXEmitter codegen)>();
        public XamlXTransformerConfiguration Configuration { get; }
        public XamlXContext RuntimeContext { get; }
        public IXamlXLocal ContextLocal { get; }
        private List<(IXamlXType type, IXamlXEmitter codeGen, IXamlXLocal local)> _localsPool = 
            new List<(IXamlXType, IXamlXEmitter, IXamlXLocal)>();

        public sealed class PooledLocal : IDisposable
        {
            public IXamlXLocal Local { get; private set; }
            private readonly XamlXEmitContext _parent;
            private readonly IXamlXType _type;
            private readonly IXamlXEmitter _codeGen;

            public PooledLocal(XamlXEmitContext parent,  IXamlXType type, IXamlXEmitter codeGen, IXamlXLocal local)
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

        public XamlXEmitContext(XamlXTransformerConfiguration configuration,
            XamlXContext runtimeContext, IXamlXLocal contextLocal,
            IEnumerable<object> emitters)
        {
            _emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
        }

        public void StLocal(XamlXAstCompilerLocalNode node,  IXamlXEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlXLoadException("Local node is assigned to a different codegen", node);
            }
            else
                _locals[node] = local = (codeGen.DefineLocal(node.Type), codeGen);

            codeGen.Emit(OpCodes.Stloc, local.local);
        }

        public void LdLocal(XamlXAstCompilerLocalNode node, IXamlXEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlXLoadException("Local node is assigned to a different codegen", node);
                codeGen.Emit(OpCodes.Ldloc, local.local);
            }
            else
                throw new XamlXLoadException("Attempt to read uninitialized local variable", node);
        }

        public PooledLocal GetLocal(IXamlXEmitter codeGen, IXamlXType type)
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