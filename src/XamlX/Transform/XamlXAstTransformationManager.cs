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
                    new XamlXKnownContentDirectivesTransformer(),
                    new XamlXArgumentsTransformer(),
                    new XamlTypeReferenceResolver(),
                    new XamlPropertyReferenceResolver(),
                    new XamlXContentTransformer(),
                    new XamlXXamlPropertyValueTransformer()
                };
                Emitters = new List<IXamlAstNodeEmitter>()
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

        public XamlXAstRootInstanceNode Transform(XamlXAstRootInstanceNode root,  bool strict = true)
        {
            var ctx = new XamlAstTransformationContext(_configuration, strict);

            foreach (var transformer in Transformers)
            {
                root = (XamlXAstRootInstanceNode) root.Visit(n => transformer.Transform(ctx, n));
            }

            return root;
        }



        public void Compile(XamlXAstRootInstanceNode root, IXamlXCodeGen codeGen)
        {
            new XamlEmitContext(_configuration, Emitters).Emit(root, codeGen);
            codeGen.Generator.Emit(OpCodes.Ret);
        }
    }


    
    public class XamlAstTransformationContext
    {
        private Dictionary<Type, object> _items = new Dictionary<Type, object>();
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();      
        public XamlTransformerConfiguration Configuration { get; }
        public bool StrictMode { get; }

        public XamlAstTransformationContext(XamlTransformerConfiguration configuration, bool strictMode = true)
        {
            Configuration = configuration;
            StrictMode = strictMode;
        }

        public T GetItem<T>() => (T) _items[typeof(T)];
        public void SetItem<T>(T item) => _items[typeof(T)] = item;       
    }


    public class XamlEmitContext
    {
        private readonly List<object> _emitters;
        public XamlTransformerConfiguration Configuration { get; }

        public XamlEmitContext(XamlTransformerConfiguration configuration, IEnumerable<object> emitters)
        {
            _emitters = emitters.ToList();
            Configuration = configuration;
        }

        public void Emit(IXamlAstNode value, IXamlXCodeGen codeGen)
        {
            foreach(var e in _emitters)
                if(e is IXamlAstNodeEmitter ve 
                && ve.Emit(value, this, codeGen))
                    return;
            throw new XamlLoadException("Unable to find emitter for node type: " + value.GetType().FullName, value);
        }
    }

    public interface IXamlAstTransformer
    {
        IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node);
    }
   
    public interface IXamlAstNodeEmitter
    {
        bool Emit(IXamlAstNode node, XamlEmitContext context, IXamlXCodeGen codeGen);
    }
    
}