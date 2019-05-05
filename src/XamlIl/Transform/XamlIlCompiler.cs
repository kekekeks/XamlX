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
                    new XamlIlMarkupExtensionTransformer(),
                    new XamlIlPropertyReferenceResolver(),
                    new XamlIlContentConvertTransformer(),
                    new XamlIlResolveContentPropertyTransformer(),
                    new XamlIlResolvePropertyValueAddersTransformer(),
                    new XamlIlConvertPropertyValuesToAssignmentsTransformer(),
                    new XamlIlNewObjectTransformer(),
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

        public XamlIlAstTransformationContext CreateTransformationContext(XamlIlDocument doc, bool strict)
            => new XamlIlAstTransformationContext(_configuration, doc.NamespaceAliases, strict);
        
        public void Transform(XamlIlDocument doc,bool strict = true)
        {
            var ctx = CreateTransformationContext(doc, strict);

            var root = doc.Root;
            ctx.RootObject = new XamlIlRootObjectNode((XamlIlAstObjectNode)root);
            foreach (var transformer in Transformers)
            {
                ctx.VisitChildren(ctx.RootObject, transformer);
                root = ctx.Visit(root, transformer);
            }

            foreach (var simplifier in SimplificationTransformers)
                root = ctx.Visit(root, simplifier);

            doc.Root = root;
        }

        XamlIlEmitContext InitCodeGen(
            IFileSource file,
            Func<string, IXamlIlType, IXamlIlTypeBuilder> createSubType,
            IXamlIlEmitter codeGen, XamlIlContext context, bool needContextLocal)
        {
            IXamlIlLocal contextLocal = null;

            if (needContextLocal)
            {
                contextLocal = codeGen.DefineLocal(context.ContextType);
                // Pass IService provider as the first argument to context factory
                codeGen
                    .Emit(OpCodes.Ldarg_0);
                context.Factory(codeGen);
                codeGen.Emit(OpCodes.Stloc, contextLocal);
            }

            var emitContext = new XamlIlEmitContext(codeGen, _configuration, context, contextLocal, createSubType, file, Emitters);
            return emitContext;
        }
        
        void CompileBuild(
            IFileSource fileSource,
            IXamlIlAstValueNode rootInstance, Func<string, IXamlIlType, IXamlIlTypeBuilder> createSubType,
            IXamlIlEmitter codeGen, XamlIlContext context, IXamlIlMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlIlAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
            var emitContext = InitCodeGen(fileSource, createSubType, codeGen, context, needContextLocal);


            var rv = codeGen.DefineLocal(rootInstance.Type.GetClrType());
            emitContext.Emit(rootInstance, codeGen, rootInstance.Type.GetClrType());
            codeGen
                .Emit(OpCodes.Stloc, rv)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldloc, rv)
                .EmitCall(compiledPopulate)
                .Emit(OpCodes.Ldloc, rv)
                .Emit(OpCodes.Ret);
        }

        /// <summary>
        /// void Populate(IServiceProvider sp, T target);
        /// </summary>

        void CompilePopulate(IFileSource fileSource, IXamlIlAstManipulationNode manipulation, Func<string, IXamlIlType, IXamlIlTypeBuilder> createSubType, IXamlIlEmitter codeGen, XamlIlContext context)
        {
            var emitContext = InitCodeGen(fileSource, createSubType, codeGen, context, true);

            codeGen
                .Emit(OpCodes.Ldloc, emitContext.ContextLocal)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, context.RootObjectField)
                .Emit(OpCodes.Ldarg_1);
            emitContext.Emit(manipulation, codeGen, null);
            codeGen.Emit(OpCodes.Ret);
        }

        public IXamlIlType CreateContextType(IXamlIlTypeBuilder builder)
        {
            return XamlIlContextDefinition.GenerateContextClass(builder,
                _configuration.TypeSystem,
                _configuration.TypeMappings);
        }
        
        public void Compile( XamlIlDocument doc, IXamlIlTypeBuilder typeBuilder, IXamlIlType contextType,
            string populateMethodName, string createMethodName, string namespaceInfoClassName,
            string baseUri, IFileSource fileSource)
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
            
            var populateMethod = typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                populateMethodName, true, true, false);

            IXamlIlTypeBuilder CreateSubType(string name, IXamlIlType baseType) 
                => typeBuilder.DefineSubType(baseType, name, false);


            var context = new XamlIlContext(contextType, rootGrp.Type.GetClrType(),
                baseUri, staticProviders);
            
            CompilePopulate(fileSource, rootGrp.Manipulation, CreateSubType, populateMethod.Generator, context);

            if (createMethodName != null)
            {
                var createMethod = typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                    new[] {_configuration.TypeMappings.ServiceProvider}, createMethodName, true, true, false);
                CompileBuild(fileSource, rootGrp.Value, CreateSubType, createMethod.Generator, context, populateMethod);
            }

            namespaceInfoBuilder?.CreateType();
        }
        
        
        
    }


    public interface IXamlIlAstTransformer
    {
        IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node);
    }

    public class XamlIlNodeEmitResult
    {
        public int ConsumedItems { get; }
        public IXamlIlType ReturnType { get; set; }
        public int ProducedItems => ReturnType == null ? 0 : 1;
        public bool AllowCast { get; set; }

        public XamlIlNodeEmitResult(int consumedItems, IXamlIlType returnType = null)
        {
            ConsumedItems = consumedItems;
            ReturnType = returnType;
        }

        public static XamlIlNodeEmitResult Void(int consumedItems) => new XamlIlNodeEmitResult(consumedItems);

        public static XamlIlNodeEmitResult Type(int consumedItems, IXamlIlType type) =>
            new XamlIlNodeEmitResult(consumedItems, type);
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
