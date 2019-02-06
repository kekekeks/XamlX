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
                    new XamlNewObjectTransformer(),
                    new XamlXXamlPropertyValueTransformer()
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
                    new MarkupExtensionEmitter()
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
            }

            doc.Root = root;
        }
        
        
        /// <summary>
        /// populate = true:
        /// void Populate(IServiceProvider sp, T target);
        /// populate = false
        /// T Build(IServiceProvider sp); 
        /// </summary>

        public void Compile(IXamlAstNode root, IXamlXCodeGen codeGen, XamlContext context, bool populate)
        {
            var contextLocal = codeGen.Generator.DefineLocal(context.ContextType);
            codeGen.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Newobj, context.Constructor)
                .Emit(OpCodes.Stloc, contextLocal);
            var rootGrp = (XamlValueWithManipulationNode) root;
            var emitContext = new XamlEmitContext(_configuration, context, contextLocal, Emitters);
            
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
        public XamlTransformerConfiguration Configuration { get; }
        public XamlContext RuntimeContext { get; }
        public IXamlLocal ContextLocal { get; }

        public XamlEmitContext(XamlTransformerConfiguration configuration,
            XamlContext runtimeContext, IXamlLocal contextLocal,
            IEnumerable<object> emitters)
        {
            _emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
        }

        public XamlNodeEmitResult Emit(IXamlAstNode value, IXamlXCodeGen codeGen, IXamlType expectedType)
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

                if (!expectedType.IsAssignableFrom(returnedType))
                {
                    throw new XamlLoadException(
                        $"Emit of node {value} resulted in  {returnedType.GetFqn()} which is not convertible to expected {expectedType.GetFqn()}",
                        value);
                }

                if (returnedType.IsValueType && !expectedType.IsValueType)
                    codeGen.Generator.Emit(OpCodes.Box, returnedType);
            }

            return res;
        }

        private XamlNodeEmitResult EmitCore(IXamlAstNode value, IXamlXCodeGen codeGen)
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
        XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlXCodeGen codeGen);
    }

    public interface IXamlAstEmitableNode
    {
        XamlNodeEmitResult Emit(XamlEmitContext context, IXamlXCodeGen codeGen);
    }
    
}