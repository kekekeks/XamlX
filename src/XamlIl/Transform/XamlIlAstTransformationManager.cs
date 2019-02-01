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
                    new XamlIlKnownContentDirectivesTransformer(),
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

        public XamlIlAstRootInstanceNode Transform(XamlIlAstRootInstanceNode root,  bool strict = true)
        {
            var ctx = new XamlIlAstTransformationContext(_configuration, strict);

            foreach (var transformer in Transformers)
            {
                root = (XamlIlAstRootInstanceNode) root.Visit(n => transformer.Transform(ctx, n));
            }

            return root;
        }



        public void Compile(XamlIlAstRootInstanceNode root, IXamlIlCodeGen codeGen)
        {
            new XamlIlEmitContext(_configuration, Emitters).Emit(root, codeGen);
            codeGen.Generator.Emit(OpCodes.Ret);
        }
    }


    
    public class XamlIlAstTransformationContext
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();      
        public XamlIlTransformerConfiguration Configuration { get; }
        public bool StrictMode { get; }

        public XamlIlAstTransformationContext(XamlIlTransformerConfiguration configuration, bool strictMode = true)
        {
            Configuration = configuration;
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

        public void Emit(IXamlIlAstNode value, IXamlIlCodeGen codeGen)
        {
            foreach(var e in _emitters)
                if(e is IXamlIlAstNodeEmitter ve 
                && ve.Emit(value, this, codeGen))
                    return;
            throw new XamlIlLoadException("Unable to find emitter for node type: " + value.GetType().FullName, value);
        }
    }

    public interface IXamlIlAstTransformer
    {
        IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node);
    }
   
    public interface IXamlIlAstNodeEmitter
    {
        bool Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen);
    }
    
}