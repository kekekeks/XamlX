using System;
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
                    new XamlIlDeferredContentTransformer(),
                    new XamlIlTopDownInitializationTransformer(),
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


        XamlIlEmitContext InitCodeGen(Func<string, IXamlIlType, IXamlIlTypeBuilder> createSubType,
            IXamlIlEmitter codeGen, XamlIlContext context, bool needContextLocal)
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

            var emitContext = new XamlIlEmitContext(codeGen, _configuration, context, contextLocal, createSubType, Emitters);
            return emitContext;
        }
        
        void CompileBuild(IXamlIlAstValueNode rootInstance, Func<string, IXamlIlType, IXamlIlTypeBuilder> createSubType,
            IXamlIlEmitter codeGen, XamlIlContext context, IXamlIlMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlIlAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
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

        void CompilePopulate(IXamlIlAstManipulationNode manipulation, Func<string, IXamlIlType, IXamlIlTypeBuilder> createSubType, IXamlIlEmitter codeGen, XamlIlContext context)
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

        public void Compile(XamlIlDocument doc, IXamlIlTypeBuilder typeBuilder,
            string populateMethodName, string createMethodName, string contextClassName, string namespaceInfoClassName,
            string baseUri)
        {
            var rootGrp = (XamlIlValueWithManipulationNode) doc.Root;
            var staticProviders = new List<IXamlIlField>();

            IXamlIlTypeBuilder namespaceInfoBuilder = null;
            if (_configuration.TypeMappings.XmlNamespaceInfoProvider != null)
            {
                namespaceInfoBuilder = typeBuilder.DefineSubType(_configuration.WellKnownTypes.Object,
                    namespaceInfoClassName, false);
                staticProviders.Add(
                    XamlIlNamespaceInfoHelper.EmitNamespaceInfoProvider(_configuration, namespaceInfoBuilder, doc));
            }
            
            var contextBuilder = typeBuilder.DefineSubType(_configuration.WellKnownTypes.Object,
                contextClassName, false);

            var contextType = XamlIlContext.GenerateContextClass(contextBuilder, _configuration.TypeSystem,
                _configuration.TypeMappings, rootGrp.Type.GetClrType(), staticProviders, baseUri);

            var populateMethod = typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                populateMethodName, true, true, false);

            IXamlIlTypeBuilder CreateSubType(string name, IXamlIlType baseType) 
                => typeBuilder.DefineSubType(baseType, name, false);

            CompilePopulate(rootGrp.Manipulation, CreateSubType, populateMethod.Generator, contextType);

            var createMethod = typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] {_configuration.TypeMappings.ServiceProvider}, createMethodName, true, true, false);
            CompileBuild(rootGrp.Value, CreateSubType, createMethod.Generator, contextType, populateMethod);
            namespaceInfoBuilder?.CreateType();
            contextType.CreateAllTypes();
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