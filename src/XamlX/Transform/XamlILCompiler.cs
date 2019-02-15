using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform.Emitters;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlILCompiler
    {
        private readonly XamlTransformerConfiguration _configuration;
        public List<IXamlAstTransformer> Transformers { get; } = new List<IXamlAstTransformer>();
        public List<IXamlAstTransformer> SimplificationTransformers { get; } = new List<IXamlAstTransformer>();
        public List<IXamlAstNodeEmitter> Emitters { get; } = new List<IXamlAstNodeEmitter>();
        public XamlILCompiler(XamlTransformerConfiguration configuration, bool fillWithDefaults)
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
                    new XamlContentConvertTransformer(),
                    new XamlNewObjectTransformer(),
                    new XamlXXamlPropertyValueTransformer(),
                    new XamlDeferredContentTransformer(),
                    new XamlTopDownInitializationTransformer(),
                };
                SimplificationTransformers = new List<IXamlAstTransformer>
                {
                    new XamlFlattenTransformer()
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
                    new MarkupExtensionEmitter(),
                    new ObjectInitializationNodeEmitter()
                };
            }
        }

        public XamlAstTransformationContext CreateTransformationContext(XamlDocument doc, bool strict)
            => new XamlAstTransformationContext(_configuration, doc.NamespaceAliases, strict);
        
        public void Transform(XamlDocument doc,bool strict = true)
        {
            var ctx = CreateTransformationContext(doc, strict);

            var root = doc.Root;
            ctx.RootObject = new XamlRootObjectNode((XamlAstObjectNode)root);
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


        XamlEmitContext InitCodeGen(Func<string, IXamlType, IXamlTypeBuilder> createSubType,
            IXamlILEmitter codeGen, XamlContext context, bool needContextLocal)
        {
            IXamlLocal contextLocal = null;

            if (needContextLocal)
            {
                contextLocal = codeGen.DefineLocal(context.ContextType);
                codeGen
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, context.Constructor)
                    .Emit(OpCodes.Stloc, contextLocal);
            }

            var emitContext = new XamlEmitContext(codeGen, _configuration, context, contextLocal, createSubType, Emitters);
            return emitContext;
        }
        
        void CompileBuild(IXamlAstValueNode rootInstance, Func<string, IXamlType, IXamlTypeBuilder> createSubType,
            IXamlILEmitter codeGen, XamlContext context, IXamlMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
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

        void CompilePopulate(IXamlAstManipulationNode manipulation, Func<string, IXamlType, IXamlTypeBuilder> createSubType, IXamlILEmitter codeGen, XamlContext context)
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

        public void Compile(XamlDocument doc, IXamlTypeBuilder typeBuilder,
            string populateMethodName, string createMethodName, string contextClassName, string namespaceInfoClassName,
            string baseUri)
        {
            var rootGrp = (XamlValueWithManipulationNode) doc.Root;
            var staticProviders = new List<IXamlField>();

            IXamlTypeBuilder namespaceInfoBuilder = null;
            if (_configuration.TypeMappings.XmlNamespaceInfoProvider != null)
            {
                namespaceInfoBuilder = typeBuilder.DefineSubType(_configuration.WellKnownTypes.Object,
                    namespaceInfoClassName, false);
                staticProviders.Add(
                    XamlNamespaceInfoHelper.EmitNamespaceInfoProvider(_configuration, namespaceInfoBuilder, doc));
            }
            
            var contextBuilder = typeBuilder.DefineSubType(_configuration.WellKnownTypes.Object,
                contextClassName, false);

            var contextType = XamlContext.GenerateContextClass(contextBuilder, _configuration.TypeSystem,
                _configuration.TypeMappings, rootGrp.Type.GetClrType(), staticProviders, baseUri);

            var populateMethod = typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                populateMethodName, true, true, false);

            IXamlTypeBuilder CreateSubType(string name, IXamlType baseType) 
                => typeBuilder.DefineSubType(baseType, name, false);

            CompilePopulate(rootGrp.Manipulation, CreateSubType, populateMethod.Generator, contextType);

            var createMethod = typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] {_configuration.TypeMappings.ServiceProvider}, createMethodName, true, true, false);
            CompileBuild(rootGrp.Value, CreateSubType, createMethod.Generator, contextType, populateMethod);
            namespaceInfoBuilder?.CreateType();
            contextType.CreateAllTypes();
        }
    }


    public interface IXamlAstTransformer
    {
        IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node);
    }

    public class XamlNodeEmitResult
    {
        public int ConsumedItems { get; }
        public IXamlType ReturnType { get; set; }
        public int ProducedItems => ReturnType == null ? 0 : 1;
        public bool AllowCast { get; set; }

        public XamlNodeEmitResult(int consumedItems, IXamlType returnType = null)
        {
            ConsumedItems = consumedItems;
            ReturnType = returnType;
        }

        public static XamlNodeEmitResult Void(int consumedItems) => new XamlNodeEmitResult(consumedItems);

        public static XamlNodeEmitResult Type(int consumedItems, IXamlType type) =>
            new XamlNodeEmitResult(consumedItems, type);
    }
    
    public interface IXamlAstNodeEmitter
    {
        XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen);
    }

    public interface IXamlAstEmitableNode
    {
        XamlNodeEmitResult Emit(XamlEmitContext context, IXamlILEmitter codeGen);
    }
    
}
