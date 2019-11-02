using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.IL;
using XamlX.Transform;
using XamlX.IL.Emitters;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlILCompiler : XamlCompiler<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public List<IXamlILAstNodeEmitter> Emitters { get; } = new List<IXamlILAstNodeEmitter>();
        
        public bool EnableIlVerification
        {
            get => _configuration.GetExtra<ILEmitContextSettings>()?.EnableILVerification ?? false;
            set
            {
                ILEmitContextSettings settings;
                if ((settings = _configuration.GetExtra<ILEmitContextSettings>()) is null)
                {
                    _configuration.AddExtra(new ILEmitContextSettings
                    {
                        EnableILVerification = value
                    });
                }
                else
                {
                    settings.EnableILVerification = value;
                }
            }
        }
        public XamlILCompiler(XamlTransformerConfiguration configuration, XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings, bool fillWithDefaults)
            : base(configuration, emitMappings, fillWithDefaults)
        {
            if (fillWithDefaults)
            {
                Transformers.AddRange(new IXamlAstTransformer[]
                {
                    new XamlNewObjectTransformer(),
                    new XamlDeferredContentTransformer(),
                    new XamlTopDownInitializationTransformer(),
                });
                
                Emitters = new List<IXamlILAstNodeEmitter>()
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

        public IXamlType CreateContextType(IXamlTypeBuilder<IXamlILEmitter> builder)
        {
            return XamlILContextDefinition.GenerateContextClass(builder,
                _configuration.TypeSystem,
                _configuration.TypeMappings,
                _emitMappings);
        }

        protected override XamlXEmitContext<IXamlILEmitter, XamlILNodeEmitResult> InitCodeGen(
            IFileSource file,
            Func<string, IXamlType, IXamlTypeBuilder<IXamlILEmitter>> createSubType,
            IXamlILEmitter codeGen, XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> context, bool needContextLocal)
        {
            IXamlLocal contextLocal = null;

            if (needContextLocal)
            {
                contextLocal = codeGen.DefineLocal(context.ContextType);
                // Pass IService provider as the first argument to context factory
                codeGen
                    .Emit(OpCodes.Ldarg_0);
                context.Factory(codeGen);
                codeGen.Emit(OpCodes.Stloc, contextLocal);
            }

            var emitContext = new XamlEmitContext(codeGen, _configuration,
                _emitMappings, context, contextLocal, createSubType,
                file, Emitters);
            return emitContext;
        }
        
        void CompileBuild(
            IFileSource fileSource,
            IXamlAstValueNode rootInstance, Func<string, IXamlType, IXamlTypeBuilder<IXamlILEmitter>> createSubType,
            IXamlILEmitter codeGen, RuntimeContext context, IXamlMethod compiledPopulate)
        {
            var needContextLocal = !(rootInstance is XamlAstNewClrObjectNode newObj && newObj.Arguments.Count == 0);
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

        void CompilePopulate(IFileSource fileSource, IXamlAstManipulationNode manipulation, Func<string, IXamlType, IXamlTypeBuilder<IXamlILEmitter>> createSubType, IXamlILEmitter codeGen, RuntimeContext context)
        {
            // Uncomment to inspect generated IL in debugger
            //codeGen = new RecordingIlEmitter(codeGen);
            var emitContext = InitCodeGen(fileSource, createSubType, codeGen, context, true);

            codeGen
                .Emit(OpCodes.Ldloc, emitContext.ContextLocal)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, context.RootObjectField)
                .Emit(OpCodes.Ldloc, emitContext.ContextLocal)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, context.IntermediateRootObjectField)
                .Emit(OpCodes.Ldarg_1);
            emitContext.Emit(manipulation, codeGen, null);
            codeGen.Emit(OpCodes.Ret);
        }

        public IXamlMethodBuilder<IXamlILEmitter> DefinePopulateMethod(IXamlTypeBuilder<IXamlILEmitter> typeBuilder,
            XamlDocument doc,
            string name, bool isPublic)
        {
            var rootGrp = (XamlValueWithManipulationNode) doc.Root;
            return typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] {_configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType()},
                name, isPublic, true, false);
        }

        public IXamlMethodBuilder<IXamlILEmitter> DefineBuildMethod(IXamlTypeBuilder<IXamlILEmitter> typeBuilder,
            XamlDocument doc,
            string name, bool isPublic)
        {
            var rootGrp = (XamlValueWithManipulationNode) doc.Root;
            return typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] {_configuration.TypeMappings.ServiceProvider}, name, isPublic, true, false);
        }
        
        public void Compile(XamlDocument doc, IXamlTypeBuilder<IXamlILEmitter> typeBuilder, IXamlType contextType,
            string populateMethodName, string createMethodName, string namespaceInfoClassName,
            string baseUri, IFileSource fileSource)
        {
            var rootGrp = (XamlValueWithManipulationNode) doc.Root;
            Compile(doc, contextType,
                DefinePopulateMethod(typeBuilder, doc, populateMethodName, true),
                createMethodName == null ?
                    null :
                    DefineBuildMethod(typeBuilder, doc, createMethodName, true),
                _configuration.TypeMappings.XmlNamespaceInfoProvider == null ?
                    null :
                    typeBuilder.DefineSubType(_configuration.WellKnownTypes.Object,
                        namespaceInfoClassName, false),
                (name, bt) => typeBuilder.DefineSubType(bt, name, false),
                baseUri, fileSource);

        }
        
        public void Compile(XamlDocument doc, IXamlType contextType,
            IXamlMethodBuilder<IXamlILEmitter> populateMethod, IXamlMethodBuilder<IXamlILEmitter> buildMethod,
            IXamlTypeBuilder<IXamlILEmitter> namespaceInfoBuilder,
            Func<string, IXamlType, IXamlTypeBuilder<IXamlILEmitter>> createClosure,
            string baseUri, IFileSource fileSource)
        {
            var rootGrp = (XamlValueWithManipulationNode) doc.Root;
            var staticProviders = new List<IXamlField>();

            if (namespaceInfoBuilder != null)
            {
                staticProviders.Add(
                    IL.XamlNamespaceInfoHelper.EmitNamespaceInfoProvider(_configuration, namespaceInfoBuilder, doc));
            }

            var context = new RuntimeContext(contextType,
                rootGrp.Type.GetClrType(), _emitMappings, baseUri, staticProviders);

            CompilePopulate(fileSource, rootGrp.Manipulation, createClosure, populateMethod.Generator, context);

            if (buildMethod != null)
            {
                CompileBuild(fileSource, rootGrp.Value, null, buildMethod.Generator, context, populateMethod);
            }

            namespaceInfoBuilder?.CreateType();
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlILNodeEmitResult : IXamlEmitResult
    {
        public int ConsumedItems { get; }
        public IXamlType ReturnType { get; set; }
        public int ProducedItems => ReturnType == null ? 0 : 1;
        public bool AllowCast { get; set; }

        public XamlILNodeEmitResult(int consumedItems, IXamlType returnType = null)
        {
            ConsumedItems = consumedItems;
            ReturnType = returnType;
        }

        public static XamlILNodeEmitResult Void(int consumedItems) => new XamlILNodeEmitResult(consumedItems);

        public static XamlILNodeEmitResult Type(int consumedItems, IXamlType type) =>
            new XamlILNodeEmitResult(consumedItems, type);
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlILAstNodeEmitter : IXamlAstNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstILEmitableNode : IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
    }
    
}
