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
        public List<object> Emitters { get; }

        private readonly Dictionary<XamlIlAstCompilerLocalNode, IXamlIlLocal>
            _locals = new Dictionary<XamlIlAstCompilerLocalNode, IXamlIlLocal>();
        
        public XamlIlTransformerConfiguration Configuration { get; }
        public XamlIlContext RuntimeContext { get; }
        public IXamlIlLocal ContextLocal { get; }
        public Func<string, IXamlIlType, IXamlIlTypeBuilder> CreateSubType { get; }
        public IXamlIlEmitter Emitter { get; }

        private readonly List<(IXamlIlType type, IXamlIlLocal local)> _localsPool =
            new List<(IXamlIlType, IXamlIlLocal)>();

        public sealed class PooledLocal : IDisposable
        {
            public IXamlIlLocal Local { get; private set; }
            private readonly XamlIlEmitContext _parent;
            private readonly IXamlIlType _type;

            public PooledLocal(XamlIlEmitContext parent, IXamlIlType type, IXamlIlLocal local)
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

        public XamlIlEmitContext(IXamlIlEmitter emitter, XamlIlTransformerConfiguration configuration,
            XamlIlContext runtimeContext, IXamlIlLocal contextLocal, 
            Func<string, IXamlIlType, IXamlIlTypeBuilder> createSubType, IEnumerable<object> emitters)
        {
            Emitter = emitter;
            Emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
            CreateSubType = createSubType;
        }

        public void StLocal(XamlIlAstCompilerLocalNode node, IXamlIlEmitter codeGen)
        {
            if (!_locals.TryGetValue(node, out var local))
                _locals[node] = local = codeGen.DefineLocal(node.Type);

            codeGen.Emit(OpCodes.Stloc, local);
        }

        public void LdLocal(XamlIlAstCompilerLocalNode node, IXamlIlEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
                codeGen.Emit(OpCodes.Ldloc, local);
            else
                throw new XamlIlLoadException("Attempt to read uninitialized local variable", node);
        }

        public PooledLocal GetLocal(IXamlIlType type)
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
        
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode value, IXamlIlEmitter codeGen, IXamlIlType expectedType)
        {
            var parent = codeGen as CheckingIlEmitter;
            parent?.Pause();
            var checkedEmitter = new CheckingIlEmitter(codeGen); 
            var res = EmitCore(value, checkedEmitter);
            var expectedBalance = res.ProducedItems - res.ConsumedItems;
            checkedEmitter.Check(res.ProducedItems - res.ConsumedItems);
            parent?.Resume();
            parent?.ExplicitStack(expectedBalance);

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

        private XamlIlNodeEmitResult EmitCore(IXamlIlAstNode value, IXamlIlEmitter codeGen)
        {
            XamlIlNodeEmitResult res = null;
            foreach (var e in Emitters)
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
