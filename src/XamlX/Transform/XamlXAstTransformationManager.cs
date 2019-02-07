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
        private readonly XamlXTransformerConfiguration _configuration;
        public List<IXamlXAstTransformer> Transformers { get; } = new List<IXamlXAstTransformer>();
        public List<IXamlXAstTransformer> SimplificationTransformers { get; } = new List<IXamlXAstTransformer>();
        public List<IXamlXAstNodeEmitter> Emitters { get; } = new List<IXamlXAstNodeEmitter>();
        public XamlXAstTransformationManager(XamlXTransformerConfiguration configuration, bool fillWithDefaults)
        {
            _configuration = configuration;
            if (fillWithDefaults)
            {
                Transformers = new List<IXamlXAstTransformer>
                {
                    new XamlXKnownDirectivesTransformer(),
                    new XamlXIntrinsicsTransformer(),
                    new XamlXXArgumentsTransformer(),
                    new XamlXTypeReferenceResolver(),
                    new XamlXPropertyReferenceResolver(),
                    new XamlXNewObjectTransformer(),
                    new XamlXXamlPropertyValueTransformer(),
                    new XamlXTopDownInitializationTransformer()
                };
                SimplificationTransformers = new List<IXamlXAstTransformer>
                {
                    new XamlXFlattenTransformer()
                };
                Emitters = new List<IXamlXAstNodeEmitter>()
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

        public void Transform(XamlXDocument doc,
            Dictionary<string, string> namespaceAliases, bool strict = true)
        {
            var ctx = new XamlXAstTransformationContext(_configuration, namespaceAliases, strict);

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
        /// populate = true:
        /// void Populate(IServiceProvider sp, T target);
        /// populate = false
        /// T Build(IServiceProvider sp); 
        /// </summary>

        public void Compile(IXamlXAstNode root, IXamlXCodeGen codeGen, XamlXContext context, bool populate)
        {
            var contextLocal = codeGen.Generator.DefineLocal(context.ContextType);
            codeGen.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Newobj, context.Constructor)
                .Emit(OpCodes.Stloc, contextLocal);
            var rootGrp = (XamlXValueWithManipulationNode) root;
            var emitContext = new XamlXEmitContext(_configuration, context, contextLocal, Emitters);
            
            if (populate)
            {
                codeGen.Generator
                    .Emit(OpCodes.Ldloc, contextLocal)
                    .Emit(OpCodes.Ldarg_1)
                    .Emit(OpCodes.Stfld, context.RootObjectField)
                    .Emit(OpCodes.Ldarg_1);
                emitContext.Emit(rootGrp.Manipulation, codeGen, null);
                codeGen.Generator.Emit(OpCodes.Ret);
            }
            else
            {
                codeGen.Generator.Emit(OpCodes.Ldloc, contextLocal);
                emitContext.Emit(rootGrp.Value, codeGen, rootGrp.Value.Type.GetClrType());
                codeGen.Generator
                    .Emit(OpCodes.Stfld, context.RootObjectField);

                codeGen.Generator
                    .Emit(OpCodes.Ldloc, contextLocal)
                    .Emit(OpCodes.Ldfld, context.RootObjectField)
                    .Emit(OpCodes.Dup);
                emitContext.Emit(rootGrp.Manipulation, codeGen, null);
                codeGen.Generator.Emit(OpCodes.Ret);
            }
        }
    }


    
    public class XamlXAstTransformationContext
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();      
        public XamlXTransformerConfiguration Configuration { get; }
        public bool StrictMode { get; }

        public IXamlXAstNode Error(IXamlXAstNode node, Exception e)
        {
            if (StrictMode)
                throw e;
            return node;
        }

        public IXamlXAstNode ParseError(string message, IXamlXAstNode node) =>
            Error(node, new XamlXParseException(message, node));
        
        public IXamlXAstNode ParseError(string message, IXamlXAstNode offender, IXamlXAstNode ret) =>
            Error(ret, new XamlXParseException(message, offender));

        public XamlXAstTransformationContext(XamlXTransformerConfiguration configuration,
            Dictionary<string, string> namespaceAliases, bool strictMode = true)
        {
            Configuration = configuration;
            NamespaceAliases = namespaceAliases;
            StrictMode = strictMode;
        }

        public T GetItem<T>() => (T) _items[typeof(T)];
        public void SetItem<T>(T item) => _items[typeof(T)] = item;       
    }


    public class XamlXEmitContext
    {
        private readonly List<object> _emitters;

        private readonly Dictionary<XamlXAstCompilerLocalNode, (IXamlXLocal local, IXamlXCodeGen codegen)>
            _locals = new Dictionary<XamlXAstCompilerLocalNode, (IXamlXLocal local, IXamlXCodeGen codegen)>();
        public XamlXTransformerConfiguration Configuration { get; }
        public XamlXContext RuntimeContext { get; }
        public IXamlXLocal ContextLocal { get; }

        public XamlXEmitContext(XamlXTransformerConfiguration configuration,
            XamlXContext runtimeContext, IXamlXLocal contextLocal,
            IEnumerable<object> emitters)
        {
            _emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
        }

        public void StLocal(XamlXAstCompilerLocalNode node,  IXamlXCodeGen codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlXLoadException("Local node is assigned to a different codegen", node);
            }
            else
                _locals[node] = local = (codeGen.Generator.DefineLocal(node.Type), codeGen);

            codeGen.Generator.Emit(OpCodes.Stloc, local.local);
        }

        public void LdLocal(XamlXAstCompilerLocalNode node, IXamlXCodeGen codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
            {
                if (local.codegen != codeGen)
                    throw new XamlXLoadException("Local node is assigned to a different codegen", node);
                codeGen.Generator.Emit(OpCodes.Ldloc, local.local);
            }
            else
                throw new XamlXLoadException("Attempt to read uninitialized local variable", node);
        }

        public XamlXNodeEmitResult Emit(IXamlXAstNode value, IXamlXCodeGen codeGen, IXamlXType expectedType)
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

                if (!expectedType.IsAssignableFrom(returnedType))
                {
                    throw new XamlXLoadException(
                        $"Emit of node {value} resulted in  {returnedType.GetFqn()} which is not convertible to expected {expectedType.GetFqn()}",
                        value);
                }

                if (returnedType.IsValueType && !expectedType.IsValueType)
                    codeGen.Generator.Emit(OpCodes.Box, returnedType);
            }

            return res;
        }

        private XamlXNodeEmitResult EmitCore(IXamlXAstNode value, IXamlXCodeGen codeGen)
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

    public interface IXamlXAstTransformer
    {
        IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node);
    }

    public class XamlXNodeEmitResult
    {
        public IXamlXType ReturnType { get; set; }
        public bool AllowCast { get; set; }

        public XamlXNodeEmitResult(IXamlXType returnType = null)
        {
            ReturnType = returnType;
        }
        public static XamlXNodeEmitResult Void { get; } = new XamlXNodeEmitResult();
        public static XamlXNodeEmitResult Type(IXamlXType type) => new XamlXNodeEmitResult(type);
    }
    
    public interface IXamlXAstNodeEmitter
    {
        XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXCodeGen codeGen);
    }

    public interface IXamlXAstEmitableNode
    {
        XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXCodeGen codeGen);
    }
    
}