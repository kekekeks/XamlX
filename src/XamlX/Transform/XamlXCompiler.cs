using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform.Emitters;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlXCompiler
    {
        private readonly XamlXTransformerConfiguration _configuration;
        public List<IXamlXAstTransformer> Transformers { get; } = new List<IXamlXAstTransformer>();
        public List<IXamlXAstTransformer> SimplificationTransformers { get; } = new List<IXamlXAstTransformer>();
        public List<IXamlXAstNodeEmitter> Emitters { get; } = new List<IXamlXAstNodeEmitter>();
        public XamlXCompiler(XamlXTransformerConfiguration configuration, bool fillWithDefaults)
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
                    new XamlXStructConvertTransformer(),
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
        ///         /// T Build(IServiceProvider sp); 
        /// </summary>


        XamlXEmitContext InitCodeGen(IXamlXEmitter codeGen, XamlXContext context,
            bool needContextLocal)
        {
            IXamlXLocal contextLocal = null;

            if (needContextLocal)
            {
                contextLocal = codeGen.DefineLocal(context.ContextType);
                codeGen
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, context.Constructor)
                    .Emit(OpCodes.Stloc, contextLocal);
            }

            var emitContext = new XamlXEmitContext(_configuration, context, contextLocal, Emitters);
            return emitContext;
        }
        
        void CompileBuild(IXamlXAstValueNode rootInstance, IXamlXEmitter codeGen, XamlXContext context,
            IXamlXMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlXAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
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

        void CompilePopulate(IXamlXAstManipulationNode manipulation, IXamlXEmitter codeGen, XamlXContext context)
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

        public void Compile(IXamlXAstNode root, IXamlXTypeBuilder typeBuilder, XamlXContext contextType,
            string populateMethodName, string createMethodName)
        {
            var rootGrp = (XamlXValueWithManipulationNode) root;
            var populateMethod = typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                populateMethodName, true, true, false);
            CompilePopulate(rootGrp.Manipulation, populateMethod.Generator, contextType);

            var createMethod = typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] {_configuration.TypeMappings.ServiceProvider}, createMethodName, true, true, false);
            CompileBuild(rootGrp.Value, createMethod.Generator, contextType, populateMethod);
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
        XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen);
    }

    public interface IXamlXAstEmitableNode
    {
        XamlXNodeEmitResult Emit(XamlXEmitContext context, IXamlXEmitter codeGen);
    }
    
}