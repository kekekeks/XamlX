using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Xml;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXEmitContext : XamlXContextBase
    {
        public IFileSource File { get; }
        public List<object> Emitters { get; }

        private readonly Dictionary<XamlXAstCompilerLocalNode, IXamlXLocal>
            _locals = new Dictionary<XamlXAstCompilerLocalNode, IXamlXLocal>();

        private IXamlXAstNode _currentNode;
        
        public XamlXTransformerConfiguration Configuration { get; }
        public XamlXContext RuntimeContext { get; }
        public IXamlXLocal ContextLocal { get; }
        public Func<string, IXamlXType, IXamlXTypeBuilder> CreateSubType { get; }
        public IXamlXEmitter Emitter { get; }

        public bool EnableIlVerification { get; }

        public XamlXEmitContext(IXamlXEmitter emitter, XamlXTransformerConfiguration configuration,
            XamlXContext runtimeContext, IXamlXLocal contextLocal, 
            Func<string, IXamlXType, IXamlXTypeBuilder> createSubType,
            IFileSource file, bool enableIlVerification, IEnumerable<object> emitters)
        {
            File = file;
            EnableIlVerification = enableIlVerification;
            Emitter = emitter;
            Emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
            CreateSubType = createSubType;
        }

        public void StLocal(XamlXAstCompilerLocalNode node, IXamlXEmitter codeGen)
        {
            if (!_locals.TryGetValue(node, out var local))
                _locals[node] = local = codeGen.DefineLocal(node.Type);

            codeGen.Emit(OpCodes.Stloc, local);
        }

        public void LdLocal(XamlXAstCompilerLocalNode node, IXamlXEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
                codeGen.Emit(OpCodes.Ldloc, local);
            else
                throw new XamlXLoadException("Attempt to read uninitialized local variable", node);
        }

        public XamlXLocalsPool.PooledLocal GetLocal(IXamlXType type)
        {
            return Emitter.LocalsPool.GetLocal(type);
        }

        public XamlXNodeEmitResult Emit(IXamlXAstNode value, IXamlXEmitter codeGen, IXamlXType expectedType)
        {
            XamlXNodeEmitResult res = null;
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
        
        private XamlXNodeEmitResult EmitCore(IXamlXAstNode value, IXamlXEmitter codeGen, IXamlXType expectedType)
        {
            CheckingIlEmitter parent = null;
            CheckingIlEmitter checkedEmitter = null;
            if (EnableIlVerification)
            {
                parent = codeGen as CheckingIlEmitter;

                parent?.Pause();
                checkedEmitter = new CheckingIlEmitter(codeGen);
            }
#if XAMLIL_DEBUG
            var res = EmitNode(value, checkedEmitter);
#else
            XamlXNodeEmitResult res;
            try
            {
                res = EmitNode(value, checkedEmitter ?? codeGen);
            }
            catch (Exception e) when (!(e is XmlException))
            {
                throw new XamlXLoadException(
                    "Internal compiler error while emitting node " + value + ":\n" + e, value);
            }
#endif
            if (EnableIlVerification)
            {
                var expectedBalance = res.ProducedItems - res.ConsumedItems;
                var checkResult =
                    checkedEmitter.Check(res.ProducedItems - res.ConsumedItems, false);
                if (checkResult != null)
                    throw new XamlXLoadException($"Error during IL verification: {checkResult}\n{checkedEmitter}\n",
                        value);
                parent?.Resume();
                parent?.ExplicitStack(expectedBalance);
            }

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
                    XamlXLocalsPool.PooledLocal local = null;
                    // ReSharper disable once ExpressionIsAlwaysNull
                    // Value is assigned inside the closure in certain conditions
                    
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
                    local?.Dispose();
                }

            }

            
            return res;
        }

        private XamlXNodeEmitResult EmitNode(IXamlXAstNode value, IXamlXEmitter codeGen)
        {
            if(File!=null)
                codeGen.InsertSequencePoint(File, value.Line, value.Position);
            
            XamlXNodeEmitResult res = null;
            foreach (var e in Emitters)
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
