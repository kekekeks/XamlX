using System;
using System.Collections.Generic;
using System.Linq;
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
            get => Settings.EnableILVerification;
            set => Settings.EnableILVerification = value;
        }

        public IXamlDynamicSetterContainerProvider DynamicSetterContainerProvider
        {
            get => Settings.DynamicSetterContainerProvider;
            set => Settings.DynamicSetterContainerProvider = value;
        }

        private ILEmitContextSettings Settings
            => _configuration.GetOrCreateExtra<ILEmitContextSettings>();

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
            IFileSource? file,
            IXamlTypeBuilder<IXamlILEmitter> declaringType,
            IXamlILEmitter codeGen,
            XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            bool needContextLocal)
        {
            IXamlLocal? contextLocal = null;

            if (needContextLocal)
            {
                contextLocal = codeGen.DefineLocal(context.ContextType);
                // Pass IService provider as the first argument to context factory
                codeGen
                    .Emit(OpCodes.Ldarg_0);
                context.Factory(codeGen);
                codeGen.Stloc(contextLocal);
            }

            return new ILEmitContext(codeGen, _configuration,
                _emitMappings, context, contextLocal, declaringType,
                file, Emitters);
        }

        protected override void CompileBuild(
            IFileSource? fileSource,
            IXamlAstValueNode rootInstance,
            IXamlTypeBuilder<IXamlILEmitter> declaringType,
            IXamlILEmitter codeGen,
            XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlMethod compiledPopulate)
        {
            var needContextLocal = false;
            if (rootInstance is XamlAstNewClrObjectNode newObj)
            {
                needContextLocal = newObj.Arguments.Count == 1 &&
                                   newObj.Arguments[0].Type.GetClrType().Equals(_configuration.TypeMappings.ServiceProvider);

                var ctorParams = newObj.Constructor.Parameters.Select(c => c.GetFullName());
                var args = newObj.Arguments.Select(a => a.Type.GetClrType().GetFullName());
                if (!ctorParams.SequenceEqual(args))
                {
                    throw new InvalidOperationException("Cannot compile Build method. Parameters mismatch." +
                                                        "Type needs to have a parameterless ctor or a ctor with a single IServiceProvider argument." +
                                                        "Or x:Arguments directive with matching arguments needs to be set");
                }
            }

            var emitContext = InitCodeGen(fileSource, declaringType, codeGen, context, needContextLocal);

            var rv = codeGen.DefineLocal(rootInstance.Type.GetClrType());
            emitContext.Emit(rootInstance, codeGen, rootInstance.Type.GetClrType());
            codeGen
                .Stloc(rv)
                .Ldarg_0()
                .Ldloc(rv)
                .EmitCall(compiledPopulate)
                .Ldloc(rv)
                .Ret();

            emitContext.ExecuteAfterEmitCallbacks();
        }

        /// <summary>
        /// void Populate(IServiceProvider sp, T target);
        /// </summary>
        protected override void CompilePopulate(
            IFileSource? fileSource,
            IXamlAstManipulationNode manipulation,
            IXamlTypeBuilder<IXamlILEmitter> declaringType,
            IXamlILEmitter codeGen,
            XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> context)
        {
            // Uncomment to inspect generated IL in debugger
            //codeGen = new RecordingIlEmitter(codeGen);
            var emitContext = InitCodeGen(fileSource, declaringType, codeGen, context, true);

            codeGen
                .Ldloc(emitContext.ContextLocal)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, context.RootObjectField!)
                .Ldloc(emitContext.ContextLocal)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, context.IntermediateRootObjectField!)
                .Emit(OpCodes.Ldarg_1);
            emitContext.Emit(manipulation, codeGen, null);
            codeGen.Emit(OpCodes.Ret);

            emitContext.ExecuteAfterEmitCallbacks();
        }

        protected override XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> CreateRuntimeContext(
            XamlDocument doc, IXamlType contextType,
            IXamlTypeBuilder<IXamlILEmitter>? namespaceInfoBuilder,
            string? baseUri, IXamlType rootType)
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
        public IXamlType? ReturnType { get; set; }
        public int ProducedItems => ReturnType == null ? 0 : 1;
        public bool AllowCast { get; set; }

        bool IXamlEmitResult.Valid => true;

        public XamlILNodeEmitResult(int consumedItems, IXamlType? returnType = null)
        {
            ConsumedItems = consumedItems;
            ReturnType = returnType;
        }

        public static XamlILNodeEmitResult Void(int consumedItems) => new XamlILNodeEmitResult(consumedItems);

        public static XamlILNodeEmitResult Type(int consumedItems, IXamlType? type) =>
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
