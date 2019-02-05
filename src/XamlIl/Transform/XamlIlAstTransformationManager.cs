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
                    new XamlIlContentTransformer(),
                    new XamlIlXamlPropertyValueTransformer()
                };
                Emitters = new List<IXamlIlAstNodeEmitter>()
                {
                    new NewObjectEmitter(),
                    new TextNodeEmitter(),
                    new MethodCallEmitter(),
                    new PropertyAssignmentEmitter(),
                    new PropertyValueManipulationEmitter(),
                    new ManipulationGroupEmitter()
                };
            }
        }

        public IXamlIlAstNode Transform(IXamlIlAstNode root,
            Dictionary<string, string> namespaceAliases, bool strict = true)
        {
            var ctx = new XamlIlAstTransformationContext(_configuration, namespaceAliases, strict);

            foreach (var transformer in Transformers)
            {
                root = root.Visit(n => transformer.Transform(ctx, n));
            }

            return root;
        }

        public void Compile(IXamlIlAstNode root, IXamlIlCodeGen codeGen)
        {
            new XamlIlEmitContext(_configuration, Emitters).Emit(root, codeGen,
                _configuration.TypeSystem.FindType("System.Object"));
            codeGen.Generator.Emit(OpCodes.Ret);
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
        public XamlIlTransformerConfiguration Configuration { get; }

        public XamlIlEmitContext(XamlIlTransformerConfiguration configuration, IEnumerable<object> emitters)
        {
            _emitters = emitters.ToList();
            Configuration = configuration;
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

                if (!expectedType.IsAssignableFrom(returnedType))
                {
                    throw new XamlIlLoadException(
                        $"Emit of node {value} resulted in  {returnedType.GetFqn()} which is not convertible to expected {expectedType.GetFqn()}",
                        value);
                }

                if (returnedType.IsValueType && !expectedType.IsValueType)
                    codeGen.Generator.Emit(OpCodes.Box, returnedType);
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