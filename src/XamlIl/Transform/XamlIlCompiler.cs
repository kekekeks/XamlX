using System.Collections.Generic;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.Transform.Emitters;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlCompiler
    {
        private readonly XamlIlTransformerConfiguration _configuration;
        public List<IXamlIlAstTransformer> Transformers { get; } = new List<IXamlIlAstTransformer>();
        public List<IXamlIlAstTransformer> SimplificationTransformers { get; } = new List<IXamlIlAstTransformer>();
        public List<IXamlIlAstNodeEmitter> Emitters { get; } = new List<IXamlIlAstNodeEmitter>();
        public XamlIlCompiler(XamlIlTransformerConfiguration configuration, bool fillWithDefaults)
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


        XamlIlEmitContext InitCodeGen(IXamlIlEmitter codeGen, XamlIlContext context,
            bool needContextLocal)
        {
            IXamlIlLocal contextLocal = null;

            if (needContextLocal)
            {
                contextLocal = codeGen.DefineLocal(context.ContextType);
                codeGen
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, context.Constructor)
                    .Emit(OpCodes.Stloc, contextLocal);
            }

            var emitContext = new XamlIlEmitContext(_configuration, context, contextLocal, Emitters);
            return emitContext;
        }
        
        void CompileBuild(IXamlIlAstValueNode rootInstance, IXamlIlEmitter codeGen, XamlIlContext context,
            IXamlIlMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlIlAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
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

        void CompilePopulate(IXamlIlAstManipulationNode manipulation, IXamlIlEmitter codeGen, XamlIlContext context)
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

        public void Compile(IXamlIlAstNode root, IXamlIlTypeBuilder typeBuilder, XamlIlContext contextType,
            string populateMethodName, string createMethodName)
        {
            var rootGrp = (XamlIlValueWithManipulationNode) root;
            var populateMethod = typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                populateMethodName, true, true, false);
            CompilePopulate(rootGrp.Manipulation, populateMethod.Generator, contextType);

            var createMethod = typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] {_configuration.TypeMappings.ServiceProvider}, createMethodName, true, true, false);
            CompileBuild(rootGrp.Value, createMethod.Generator, contextType, populateMethod);
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
        XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlEmitter codeGen);
    }

    public interface IXamlIlAstEmitableNode
    {
        XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen);
    }
    
}