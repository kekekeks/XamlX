using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Xml;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlEmitContext : XamlContextBase
    {
        public IFileSource File { get; }
        public List<object> Emitters { get; }

        private readonly Dictionary<XamlAstCompilerLocalNode, IXamlLocal>
            _locals = new Dictionary<XamlAstCompilerLocalNode, IXamlLocal>();

        private IXamlAstNode _currentNode;
        
        public XamlTransformerConfiguration Configuration { get; }
        public XamlContext RuntimeContext { get; }
        public IXamlLocal ContextLocal { get; }
        public Func<string, IXamlType, IXamlTypeBuilder> CreateSubType { get; }
        public IXamlILEmitter Emitter { get; }

        public XamlEmitContext(IXamlILEmitter emitter, XamlTransformerConfiguration configuration,
            XamlContext runtimeContext, IXamlLocal contextLocal, 
            Func<string, IXamlType, IXamlTypeBuilder> createSubType,
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

        public void StLocal(XamlAstCompilerLocalNode node, IXamlILEmitter codeGen)
        {
            if (!_locals.TryGetValue(node, out var local))
                _locals[node] = local = codeGen.DefineLocal(node.Type);

            codeGen.Emit(OpCodes.Stloc, local);
        }

        public void LdLocal(XamlAstCompilerLocalNode node, IXamlILEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
                codeGen.Emit(OpCodes.Ldloc, local);
            else
                throw new XamlLoadException("Attempt to read uninitialized local variable", node);
        }

        public XamlLocalsPool.PooledLocal GetLocal(IXamlType type)
        {
            return Emitter.LocalsPool.GetLocal(type);
        }

        public XamlNodeEmitResult Emit(IXamlAstNode value, IXamlILEmitter codeGen, IXamlType expectedType)
        {
            XamlNodeEmitResult res = null;
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
        
        private XamlNodeEmitResult EmitCore(IXamlAstNode value, IXamlILEmitter codeGen, IXamlType expectedType)
        {
            var parent = codeGen as CheckingIlEmitter;
            parent?.Pause();
            var checkedEmitter = new CheckingIlEmitter(codeGen); 

#if XAMLIL_DEBUG
            var res = EmitNode(value, checkedEmitter);
#else
            XamlNodeEmitResult res;
            try
            {
                res = EmitNode(value, checkedEmitter);
            }
            catch (Exception e) when (!(e is XmlException))
            {
                throw new XamlLoadException(
                    "Internal compiler error while emitting node " + value + ":\n" + e, value);
            }
#endif
            var expectedBalance = res.ProducedItems - res.ConsumedItems;
            var checkResult =
                checkedEmitter.Check(res.ProducedItems - res.ConsumedItems, false);
            if (checkResult != null)
                throw new XamlLoadException($"Error during IL verification: {checkResult}\n{checkedEmitter}\n", value);
            parent?.Resume();
            parent?.ExplicitStack(expectedBalance);

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
                    XamlLocalsPool.PooledLocal local = null;
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

        private XamlNodeEmitResult EmitNode(IXamlAstNode value, IXamlILEmitter codeGen)
        {
            if(File!=null)
                codeGen.InsertSequencePoint(File, value.Line, value.Position);
            
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
