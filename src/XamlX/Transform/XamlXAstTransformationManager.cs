using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform.Emitters;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlXAstTransformationManager
    {
        private readonly XamlTransformerConfiguration _configuration;
        public List<IXamlAstTransformer> Transformers { get; } = new List<IXamlAstTransformer>();
        public List<IXamlAstTransformer> SimplificationTransformers { get; } = new List<IXamlAstTransformer>();
        public List<IXamlAstNodeEmitter> Emitters { get; } = new List<IXamlAstNodeEmitter>();
        public XamlXAstTransformationManager(XamlTransformerConfiguration configuration, bool fillWithDefaults)
        {
            _configuration = configuration;
            if (fillWithDefaults)
            {
                Transformers = new List<IXamlAstTransformer>
                {
                    new XamlKnownDirectivesTransformer(),
                    new XamlIntrinsicsTransformer(),
                    new XamlXArgumentsTransformer(),
                    new XamlTypeReferenceResolver(),
                    new XamlPropertyReferenceResolver(),
                    new XamlXStructConvertTransformer(),
                    new XamlNewObjectTransformer(),
                    new XamlXXamlPropertyValueTransformer(),
                    new XamlTopDownInitializationTransformer()
                };
                SimplificationTransformers = new List<IXamlAstTransformer>
                {
                    new XamlFlattenTransformer()
                };
                Emitters = new List<IXamlAstNodeEmitter>()
                {
                    new NewObjectEmitter(),
                    new TextNodeEmitter(),
                    new MethodCallEmitter(),
                    new PropertyAssignmentEmitter(),
                    new PropertyValueManipulationEmitter(),
                    new ManipulationGroupEmitter(),
                    new ValueWithManipulationsEmitter(),
                    new MarkupExtensionEmitter(),
                    new ObjectInitializationNodeEmitter()
                };
            }
        }

        public void Transform(XamlDocument doc,
            Dictionary<string, string> namespaceAliases, bool strict = true)
        {
            var ctx = new XamlAstTransformationContext(_configuration, namespaceAliases, strict);

            var root = doc.Root;
            foreach (var transformer in Transformers)
            {
                root = root.Visit(n => transformer.Transform(ctx, n));
                foreach (var simplifier in SimplificationTransformers)
                    root = root.Visit(n => simplifier.Transform(ctx, n));
            }

            doc.Root = root;
        }


        /// <summary>
        ///         /// T Build(IServiceProvider sp); 
        /// </summary>


        XamlEmitContext InitCodeGen(IXamlILEmitter codeGen, XamlContext context,
            bool needContextLocal)
        {
            IXamlLocal contextLocal = null;

            if (needContextLocal)
            {
                contextLocal = codeGen.DefineLocal(context.ContextType);
                codeGen
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, context.Constructor)
                    .Emit(OpCodes.Stloc, contextLocal);
            }

            var emitContext = new XamlEmitContext(_configuration, context, contextLocal, Emitters);
            return emitContext;
        }
        
        void CompileBuild(IXamlAstValueNode rootInstance, IXamlILEmitter codeGen, XamlContext context,
            IXamlMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
            var emitContext = InitCodeGen(codeGen, context, needContextLocal);


            var rv = codeGen.DefineLocal(rootInstance.Type.GetClrType());
            emitContext.Emit(rootInstance, codeGen, rootInstance.Type.GetClrType());
            codeGen
                .Emit(OpCodes.Stloc, rv)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldloc, rv)
                .Emit(OpCodes.Call, compiledPopulate)
                .Emit(OpCodes.Ldloc, rv)
                .Emit(OpCodes.Ret);
        }

        /// <summary>
        /// void Populate(IServiceProvider sp, T target);
        /// </summary>

        void CompilePopulate(IXamlAstManipulationNode manipulation, IXamlILEmitter codeGen, XamlContext context)
        {
            var emitContext = InitCodeGen(codeGen, context, true);

            codeGen
                .Emit(OpCodes.Ldloc, emitContext.ContextLocal)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, context.RootObjectField)
                .Emit(OpCodes.Ldarg_1);
            emitContext.Emit(manipulation, codeGen, null);
            codeGen.Emit(OpCodes.Ret);
        }

        public void Compile(IXamlAstNode root, IXamlTypeBuilder typeBuilder, XamlContext contextType,
            string populateMethodName, string createMethodName)
        {
            var rootGrp = (XamlValueWithManipulationNode) root;
            var populateMethod = typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                populateMethodName, true, true, false);
            CompilePopulate(rootGrp.Manipulation, populateMethod.Generator, contextType);

            var createMethod = typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] {_configuration.TypeMappings.ServiceProvider}, createMethodName, true, true, false);
            CompileBuild(rootGrp.Value, createMethod.Generator, contextType, populateMethod);
        }
    }


    
    public class XamlAstTransformationContext
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();      
        public XamlTransformerConfiguration Configuration { get; }
        public bool StrictMode { get; }

        public IXamlAstNode Error(IXamlAstNode node, Exception e)
        {
            if (StrictMode)
                throw e;
            return node;
        }

        public IXamlAstNode ParseError(string message, IXamlAstNode node) =>
            Error(node, new XamlParseException(message, node));
        
        public IXamlAstNode ParseError(string message, IXamlAstNode offender, IXamlAstNode ret) =>
            Error(ret, new XamlParseException(message, offender));

        public XamlAstTransformationContext(XamlTransformerConfiguration configuration,
            Dictionary<string, string> namespaceAliases, bool strictMode = true)
        {
            Configuration = configuration;
            NamespaceAliases = namespaceAliases;
            StrictMode = strictMode;
        }

        public T GetItem<T>() => (T) _items[typeof(T)];
        public void SetItem<T>(T item) => _items[typeof(T)] = item;       
    }


    public class XamlEmitContext
    {
        private readonly List<object> _emitters;

        private readonly Dictionary<XamlAstCompilerLocalNode, (IXamlLocal local, IXamlILEmitter codegen)>
            _locals = new Dictionary<XamlAstCompilerLocalNode, (IXamlLocal local, IXamlILEmitter codegen)>();
        public XamlTransformerConfiguration Configuration { get; }
        public XamlContext RuntimeContext { get; }
        public IXamlLocal ContextLocal { get; }
        private List<(IXamlType type, IXamlILEmitter codeGen, IXamlLocal local)> _localsPool = 
            new List<(IXamlType, IXamlILEmitter, IXamlLocal)>();

        public sealed class PooledLocal : IDisposable
        {
            public IXamlLocal Local { get; private set; }
            private readonly XamlEmitContext _parent;
            private readonly IXamlType _type;
            private readonly IXamlILEmitter _codeGen;

            public PooledLocal(XamlEmitContext parent,  IXamlType type, IXamlILEmitter codeGen, IXamlLocal local)
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

        public XamlEmitContext(XamlTransformerConfiguration configuration,
            XamlContext runtimeContext, IXamlLocal contextLocal,
            IEnumerable<object> emitters)
        {
            _emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
        }

        public void StLocal(XamlAstCompilerLocalNode node,  IXamlILEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlLoadException("Local node is assigned to a different codegen", node);
            }
            else
                _locals[node] = local = (codeGen.DefineLocal(node.Type), codeGen);

            codeGen.Emit(OpCodes.Stloc, local.local);
        }

        public void LdLocal(XamlAstCompilerLocalNode node, IXamlILEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlLoadException("Local node is assigned to a different codegen", node);
                codeGen.Emit(OpCodes.Ldloc, local.local);
            }
            else
                throw new XamlLoadException("Attempt to read uninitialized local variable", node);
        }

        public PooledLocal GetLocal(IXamlILEmitter codeGen, IXamlType type)
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
        
        public XamlNodeEmitResult Emit(IXamlAstNode value, IXamlILEmitter codeGen, IXamlType expectedType)
        {
            var res = EmitCore(value, codeGen);
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

        private XamlNodeEmitResult EmitCore(IXamlAstNode value, IXamlILEmitter codeGen)
        {
            XamlNodeEmitResult res = null;
            foreach (var e in _emitters)
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

    public interface IXamlAstTransformer
    {
        IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node);
    }

    public class XamlNodeEmitResult
    {
        public IXamlType ReturnType { get; set; }
        public bool AllowCast { get; set; }

        public XamlNodeEmitResult(IXamlType returnType = null)
        {
            ReturnType = returnType;
        }
        public static XamlNodeEmitResult Void { get; } = new XamlNodeEmitResult();
        public static XamlNodeEmitResult Type(IXamlType type) => new XamlNodeEmitResult(type);
    }
    
    public interface IXamlAstNodeEmitter
    {
        XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen);
    }

    public interface IXamlAstEmitableNode
    {
        XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen);
    }
    
}