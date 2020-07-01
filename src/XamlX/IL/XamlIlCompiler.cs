using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.IL.Emitters;
using XamlX.TypeSystem;
using XamlX.Emit;
using XamlX.Compiler;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlILCompiler : XamlImperativeCompiler<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public bool EnableIlVerification
        {
            get => _configuration.GetOrCreateExtra<ILEmitContextSettings>().EnableILVerification;
            set => _configuration.GetOrCreateExtra<ILEmitContextSettings>().EnableILVerification = value;
        }

        public XamlILCompiler(
            TransformerConfiguration configuration,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings,
            bool fillWithDefaults)
            : base(configuration, emitMappings, fillWithDefaults)
        {
            if (fillWithDefaults)
            {                
                Emitters.AddRange(new object[]
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
                });
            }
        }

        public IXamlType CreateContextType(IXamlTypeBuilder<IXamlILEmitter> builder)
        {
            return XamlILContextDefinition.GenerateContextClass(builder,
                _configuration.TypeSystem,
                _configuration.TypeMappings,
                _emitMappings);
        }

        protected override XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> InitCodeGen(
            IFileSource file,
            Func<string, IXamlType, IXamlTypeBuilder<IXamlILEmitter>> createSubType,
            IXamlILEmitter codeGen, XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            bool needContextLocal)
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

            var emitContext = new ILEmitContext(codeGen, _configuration,
                _emitMappings, context, contextLocal, createSubType,
                file, Emitters);
            return emitContext;
        }
        
        protected override void CompileBuild(
            IFileSource fileSource,
            IXamlAstValueNode rootInstance, Func<string, IXamlType, IXamlTypeBuilder<IXamlILEmitter>> createSubType,
            IXamlILEmitter codeGen, XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlMethod compiledPopulate)
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
        protected override void CompilePopulate(IFileSource fileSource, IXamlAstManipulationNode manipulation,
            Func<string, IXamlType, IXamlTypeBuilder<IXamlILEmitter>> createSubType,
            IXamlILEmitter codeGen, XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> context)
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

        protected override XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> CreateRuntimeContext(
            XamlDocument doc, IXamlType contextType,
            IXamlTypeBuilder<IXamlILEmitter> namespaceInfoBuilder,
            string baseUri, IXamlType rootType)
        {
            var staticProviders = new List<IXamlField>();

            if (namespaceInfoBuilder != null)
            {
                staticProviders.Add(
                    NamespaceInfoProvider.EmitNamespaceInfoProvider(_configuration, namespaceInfoBuilder, doc));
            }

            var context = new RuntimeContext(contextType,
                rootType, _emitMappings, baseUri, staticProviders);
            return context;
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

        bool IXamlEmitResult.Valid => true;

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
