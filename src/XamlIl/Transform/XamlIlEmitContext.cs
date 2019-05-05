using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlEmitContext : XamlIlContextBase
    {
        public IFileSource File { get; }
        public List<object> Emitters { get; }

        private readonly Dictionary<XamlIlAstCompilerLocalNode, IXamlIlLocal>
            _locals = new Dictionary<XamlIlAstCompilerLocalNode, IXamlIlLocal>();

        private IXamlIlAstNode _currentNode;
        
        public XamlIlTransformerConfiguration Configuration { get; }
        public XamlIlContext RuntimeContext { get; }
        public IXamlIlLocal ContextLocal { get; }
        public Func<string, IXamlIlType, IXamlIlTypeBuilder> CreateSubType { get; }
        public IXamlIlEmitter Emitter { get; }

        public XamlIlEmitContext(IXamlIlEmitter emitter, XamlIlTransformerConfiguration configuration,
            XamlIlContext runtimeContext, IXamlIlLocal contextLocal, 
            Func<string, IXamlIlType, IXamlIlTypeBuilder> createSubType,
            IFileSource file, IEnumerable<object> emitters)
        {
            File = file;
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

        public XamlIlLocalsPool.PooledLocal GetLocal(IXamlIlType type)
        {
            return Emitter.LocalsPool.GetLocal(type);
        }

        public XamlIlNodeEmitResult Emit(IXamlIlAstNode value, IXamlIlEmitter codeGen, IXamlIlType expectedType)
        {
            XamlIlNodeEmitResult res = null;
            if (_currentNode != null)
            {
                PushParent(_currentNode);
                _currentNode = value;
                res = EmitCore(value, codeGen, expectedType);
                _currentNode = PopParent();
            }
            else
            {
                _currentNode = value;
                res = EmitCore(value, codeGen, expectedType);
                _currentNode = null;
            }

            return res;
        }
        
        private XamlIlNodeEmitResult EmitCore(IXamlIlAstNode value, IXamlIlEmitter codeGen, IXamlIlType expectedType)
        {
            var parent = codeGen as CheckingIlEmitter;
            parent?.Pause();
            var checkedEmitter = new CheckingIlEmitter(codeGen); 
            var res = EmitNode(value, checkedEmitter);
            var expectedBalance = res.ProducedItems - res.ConsumedItems;
            var checkResult =
                checkedEmitter.Check(res.ProducedItems - res.ConsumedItems, false);
            if (checkResult != null)
                throw new XamlIlLoadException($"Error during IL verification: {checkResult}\n{checkedEmitter}\n", value);
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
                    XamlIlLocalsPool.PooledLocal local = null;
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

        private XamlIlNodeEmitResult EmitNode(IXamlIlAstNode value, IXamlIlEmitter codeGen)
        {
            if(File!=null)
                codeGen.InsertSequencePoint(File, value.Line, value.Position);
            
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
