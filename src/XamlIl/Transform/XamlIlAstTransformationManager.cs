using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.Transform.Emitters;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlAstTransformationManager
    {
        private readonly XamlIlTransformerConfiguration _configuration;
        public List<IXamlIlAstTransformer> Transformers { get; } = new List<IXamlIlAstTransformer>();
        public List<IXamlIlAstTransformer> SimplificationTransformers { get; } = new List<IXamlIlAstTransformer>();
        public List<IXamlIlAstNodeEmitter> Emitters { get; } = new List<IXamlIlAstNodeEmitter>();
        public XamlIlAstTransformationManager(XamlIlTransformerConfiguration configuration, bool fillWithDefaults)
        {
            _configuration = configuration;
            if (fillWithDefaults)
            {
                Transformers = new List<IXamlIlAstTransformer>
                {
                    new XamlIlKnownDirectivesTransformer(),
                    new XamlIlIntrinsicsTransformer(),
                    new XamlIlXArgumentsTransformer(),
                    new XamlIlTypeReferenceResolver(),
                    new XamlIlPropertyReferenceResolver(),
                    new XamlIlStructConvertTransformer(),
                    new XamlIlNewObjectTransformer(),
                    new XamlIlXamlPropertyValueTransformer(),
                    new XamlIlTopDownInitializationTransformer()
                };
                SimplificationTransformers = new List<IXamlIlAstTransformer>
                {
                    new XamlIlFlattenTransformer()
                };
                Emitters = new List<IXamlIlAstNodeEmitter>()
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

        public void Transform(XamlIlDocument doc,
            Dictionary<string, string> namespaceAliases, bool strict = true)
        {
            var ctx = new XamlIlAstTransformationContext(_configuration, namespaceAliases, strict);

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


        XamlIlEmitContext InitCodeGen(IXamlIlCodeGen codeGen, XamlIlContext context,
            bool needContextLocal)
        {
            IXamlIlLocal contextLocal = null;

            if (needContextLocal)
            {
                contextLocal = codeGen.Generator.DefineLocal(context.ContextType);
                codeGen.Generator
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, context.Constructor)
                    .Emit(OpCodes.Stloc, contextLocal);
            }

            var emitContext = new XamlIlEmitContext(_configuration, context, contextLocal, Emitters);
            return emitContext;
        }
        
        void CompileBuild(IXamlIlAstValueNode rootInstance, IXamlIlCodeGen codeGen, XamlIlContext context,
            IXamlIlMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlIlAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
            var emitContext = InitCodeGen(codeGen, context, needContextLocal);


            var rv = codeGen.Generator.DefineLocal(rootInstance.Type.GetClrType());
            emitContext.Emit(rootInstance, codeGen, rootInstance.Type.GetClrType());
            codeGen.Generator
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

        void CompilePopulate(IXamlIlAstManipulationNode manipulation, IXamlIlCodeGen codeGen, XamlIlContext context)
        {
            var emitContext = InitCodeGen(codeGen, context, true);

            codeGen.Generator
                .Emit(OpCodes.Ldloc, emitContext.ContextLocal)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, context.RootObjectField)
                .Emit(OpCodes.Ldarg_1);
            emitContext.Emit(manipulation, codeGen, null);
            codeGen.Generator.Emit(OpCodes.Ret);
        }

        public void Compile(IXamlIlAstNode root, IXamlIlTypeBuilder typeBuilder, XamlIlContext contextType,
            string populateMethodName, string createMethodName)
        {
            var rootGrp = (XamlIlValueWithManipulationNode) root;
            var populateMethod = typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                populateMethodName, true, true, false);
            CompilePopulate(rootGrp.Manipulation, populateMethod, contextType);

            var createMethod = typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] {_configuration.TypeMappings.ServiceProvider}, createMethodName, true, true, false);
            CompileBuild(rootGrp.Value, createMethod, contextType, populateMethod);
        }
    }


    
    public class XamlIlAstTransformationContext
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();      
        public XamlIlTransformerConfiguration Configuration { get; }
        public bool StrictMode { get; }

        public IXamlIlAstNode Error(IXamlIlAstNode node, Exception e)
        {
            if (StrictMode)
                throw e;
            return node;
        }

        public IXamlIlAstNode ParseError(string message, IXamlIlAstNode node) =>
            Error(node, new XamlIlParseException(message, node));
        
        public IXamlIlAstNode ParseError(string message, IXamlIlAstNode offender, IXamlIlAstNode ret) =>
            Error(ret, new XamlIlParseException(message, offender));

        public XamlIlAstTransformationContext(XamlIlTransformerConfiguration configuration,
            Dictionary<string, string> namespaceAliases, bool strictMode = true)
        {
            Configuration = configuration;
            NamespaceAliases = namespaceAliases;
            StrictMode = strictMode;
        }

        public T GetItem<T>() => (T) _items[typeof(T)];
        public void SetItem<T>(T item) => _items[typeof(T)] = item;       
    }


    public class XamlIlEmitContext
    {
        private readonly List<object> _emitters;

        private readonly Dictionary<XamlIlAstCompilerLocalNode, (IXamlIlLocal local, IXamlIlCodeGen codegen)>
            _locals = new Dictionary<XamlIlAstCompilerLocalNode, (IXamlIlLocal local, IXamlIlCodeGen codegen)>();
        public XamlIlTransformerConfiguration Configuration { get; }
        public XamlIlContext RuntimeContext { get; }
        public IXamlIlLocal ContextLocal { get; }
        private List<(IXamlIlType type, IXamlIlCodeGen codeGen, IXamlIlLocal local)> _localsPool = 
            new List<(IXamlIlType, IXamlIlCodeGen, IXamlIlLocal)>();

        public sealed class PooledLocal : IDisposable
        {
            public IXamlIlLocal Local { get; private set; }
            private readonly XamlIlEmitContext _parent;
            private readonly IXamlIlType _type;
            private readonly IXamlIlCodeGen _codeGen;

            public PooledLocal(XamlIlEmitContext parent,  IXamlIlType type, IXamlIlCodeGen codeGen, IXamlIlLocal local)
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

        public void StLocal(XamlIlAstCompilerLocalNode node,  IXamlIlCodeGen codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlIlLoadException("Local node is assigned to a different codegen", node);
            }
            else
                _locals[node] = local = (codeGen.Generator.DefineLocal(node.Type), codeGen);

            codeGen.Generator.Emit(OpCodes.Stloc, local.local);
        }

        public void LdLocal(XamlIlAstCompilerLocalNode node, IXamlIlCodeGen codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlIlLoadException("Local node is assigned to a different codegen", node);
                codeGen.Generator.Emit(OpCodes.Ldloc, local.local);
            }
            else
                throw new XamlIlLoadException("Attempt to read uninitialized local variable", node);
        }

        public PooledLocal GetLocal(IXamlIlCodeGen codeGen, IXamlIlType type)
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

            return new PooledLocal(this, type, codeGen, codeGen.Generator.DefineLocal(type));

        }
        
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode value, IXamlIlCodeGen codeGen, IXamlIlType expectedType)
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
                                codeGen.Generator
                                    .Stloc(local.Local)
                                    .Ldloca(local.Local);

                            }
                            // Otherwise do nothing, value is already at the top of the stack
                            return codeGen.Generator;
                        });
                }

            }

            return res;
        }

        private XamlIlNodeEmitResult EmitCore(IXamlIlAstNode value, IXamlIlCodeGen codeGen)
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

    public interface IXamlIlAstTransformer
    {
        IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node);
    }

    public class XamlIlNodeEmitResult
    {
        public IXamlIlType ReturnType { get; set; }
        public bool AllowCast { get; set; }

        public XamlIlNodeEmitResult(IXamlIlType returnType = null)
        {
            ReturnType = returnType;
        }
        public static XamlIlNodeEmitResult Void { get; } = new XamlIlNodeEmitResult();
        public static XamlIlNodeEmitResult Type(IXamlIlType type) => new XamlIlNodeEmitResult(type);
    }
    
    public interface IXamlIlAstNodeEmitter
    {
        XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen);
    }

    public interface IXamlIlAstEmitableNode
    {
        XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlCodeGen codeGen);
    }
    
}