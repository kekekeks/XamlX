using System;
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
                    new XamlXDeferredContentTransformer(),
                    new XamlXTopDownInitializationTransformer(),
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


        XamlXEmitContext InitCodeGen(Func<string, IXamlXType, IXamlXTypeBuilder> createSubType,
            IXamlXEmitter codeGen, XamlXContext context, bool needContextLocal)
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

            var emitContext = new XamlXEmitContext(codeGen, _configuration, context, contextLocal, createSubType, Emitters);
            return emitContext;
        }
        
        void CompileBuild(IXamlXAstValueNode rootInstance, Func<string, IXamlXType, IXamlXTypeBuilder> createSubType,
            IXamlXEmitter codeGen, XamlXContext context, IXamlXMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlXAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
            var emitContext = InitCodeGen(createSubType, codeGen, context, needContextLocal);


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

        void CompilePopulate(IXamlXAstManipulationNode manipulation, Func<string, IXamlXType, IXamlXTypeBuilder> createSubType, IXamlXEmitter codeGen, XamlXContext context)
        {
            var emitContext = InitCodeGen(createSubType, codeGen, context, true);

            codeGen
                .Emit(OpCodes.Ldloc, emitContext.ContextLocal)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, context.RootObjectField)
                .Emit(OpCodes.Ldarg_1);
            emitContext.Emit(manipulation, codeGen, null);
            codeGen.Emit(OpCodes.Ret);
        }

        public void Compile(XamlXDocument doc, IXamlXTypeBuilder typeBuilder,
            string populateMethodName, string createMethodName, string contextClassName, string namespaceInfoClassName,
            string baseUri)
        {
            var rootGrp = (XamlXValueWithManipulationNode) doc.Root;
            var staticProviders = new List<IXamlXField>();

            IXamlXTypeBuilder namespaceInfoBuilder = null;
            if (_configuration.TypeMappings.XmlNamespaceInfoProvider != null)
            {
                namespaceInfoBuilder = typeBuilder.DefineSubType(_configuration.WellKnownTypes.Object,
                    namespaceInfoClassName, false);
                staticProviders.Add(
                    XamlXNamespaceInfoHelper.EmitNamespaceInfoProvider(_configuration, namespaceInfoBuilder, doc));
            }
            
            var contextBuilder = typeBuilder.DefineSubType(_configuration.WellKnownTypes.Object,
                contextClassName, false);

            var contextType = XamlXContext.GenerateContextClass(contextBuilder, _configuration.TypeSystem,
                _configuration.TypeMappings, rootGrp.Type.GetClrType(), staticProviders, baseUri);

            var populateMethod = typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                populateMethodName, true, true, false);

            IXamlXTypeBuilder CreateSubType(string name, IXamlXType baseType) 
                => typeBuilder.DefineSubType(baseType, name, false);

            CompilePopulate(rootGrp.Manipulation, CreateSubType, populateMethod.Generator, contextType);

            var createMethod = typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] {_configuration.TypeMappings.ServiceProvider}, createMethodName, true, true, false);
            CompileBuild(rootGrp.Value, CreateSubType, createMethod.Generator, contextType, populateMethod);
            namespaceInfoBuilder?.CreateType();
            contextType.CreateAllTypes();
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