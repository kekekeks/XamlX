using System;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Compiler
{
#if !XAMLX_INTERNAL
    public
#endif
    abstract class XamlImperativeCompiler<TBackendEmitter, TEmitResult> : XamlCompiler<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        public XamlImperativeCompiler(TransformerConfiguration configuration,
            XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> emitMappings, bool fillWithDefaults)
            : base(configuration, emitMappings, fillWithDefaults)
        {
            if (fillWithDefaults)
            {
                Transformers.AddRange(new IXamlAstTransformer[]
                {
                    new NewObjectTransformer(),
                    new DeferredContentTransformer(),
                    new TopDownInitializationTransformer(),
                });
            }
        }

        /// <summary>
        /// void Populate(IServiceProvider sp, T target);
        /// </summary>
        public IXamlMethodBuilder<TBackendEmitter> DefinePopulateMethod(IXamlTypeBuilder<TBackendEmitter> typeBuilder,
            XamlDocument doc,
            string name, XamlVisibility visibility)
        {
            var rootGrp = (XamlValueWithManipulationNode)doc.Root;
            return typeBuilder.DefineMethod(_configuration.WellKnownTypes.Void,
                new[] { _configuration.TypeMappings.ServiceProvider, rootGrp.Type.GetClrType() },
                name, visibility, true, false);
        }

        /// <summary>
        /// T Build(IServiceProvider sp);
        /// </summary>
        public IXamlMethodBuilder<TBackendEmitter> DefineBuildMethod(IXamlTypeBuilder<TBackendEmitter> typeBuilder,
            XamlDocument doc,
            string name, XamlVisibility visibility)
        {
            var rootGrp = (XamlValueWithManipulationNode)doc.Root;
            return typeBuilder.DefineMethod(rootGrp.Type.GetClrType(),
                new[] { _configuration.TypeMappings.ServiceProvider }, name, visibility, true, false);
        }

        public void Compile(
            XamlDocument doc,
            IXamlTypeBuilder<TBackendEmitter> typeBuilder,
            IXamlType contextType,
            string populateMethodName,
            string? createMethodName,
            string namespaceInfoClassName,
            string? baseUri,
            IFileSource? fileSource)
            => Compile(
                doc,
                contextType,
                DefinePopulateMethod(typeBuilder, doc, populateMethodName, XamlVisibility.Public),
                typeBuilder,
                createMethodName == null ?
                    null :
                    DefineBuildMethod(typeBuilder, doc, createMethodName, XamlVisibility.Public),
                typeBuilder,
                _configuration.TypeMappings.XmlNamespaceInfoProvider == null ?
                    null :
                    typeBuilder.DefineSubType(_configuration.WellKnownTypes.Object, namespaceInfoClassName, XamlVisibility.Private),
                baseUri,
                fileSource);

        public void Compile(
            XamlDocument doc,
            IXamlType contextType,
            IXamlMethodBuilder<TBackendEmitter> populateMethod,
            IXamlTypeBuilder<TBackendEmitter> populateDeclaringType,
            IXamlMethodBuilder<TBackendEmitter>? buildMethod,
            IXamlTypeBuilder<TBackendEmitter>? buildDeclaringType,
            IXamlTypeBuilder<TBackendEmitter>? namespaceInfoBuilder,
            string? baseUri,
            IFileSource? fileSource)
        {
            var rootGrp = (XamlValueWithManipulationNode)doc.Root;
            var rootType = rootGrp.Type.GetClrType();
            var context = CreateRuntimeContext(doc, contextType, namespaceInfoBuilder, baseUri, rootType);

            CompilePopulate(fileSource, rootGrp.Manipulation!, populateDeclaringType, populateMethod.Generator, context);

            if (buildMethod != null)
            {
                if (buildDeclaringType is null)
                    throw new ArgumentNullException(nameof(buildDeclaringType));

                CompileBuild(fileSource, rootGrp.Value, buildDeclaringType, buildMethod.Generator, context, populateMethod);
            }

            namespaceInfoBuilder?.CreateType();
        }

        protected abstract void CompilePopulate(
            IFileSource? fileSource,
            IXamlAstManipulationNode manipulation,
            IXamlTypeBuilder<TBackendEmitter> declaringType,
            TBackendEmitter codeGen,
            XamlRuntimeContext<TBackendEmitter, TEmitResult> context);

        protected abstract void CompileBuild(
            IFileSource? fileSource,
            IXamlAstValueNode rootInstance,
            IXamlTypeBuilder<TBackendEmitter> declaringType,
            TBackendEmitter codeGen,
            XamlRuntimeContext<TBackendEmitter, TEmitResult> context,
            IXamlMethod compiledPopulate);

        protected abstract XamlRuntimeContext<TBackendEmitter, TEmitResult> CreateRuntimeContext(
            XamlDocument doc,
            IXamlType contextType,
            IXamlTypeBuilder<TBackendEmitter>? namespaceInfoBuilder,
            string? baseUri,
            IXamlType rootType);
    }
}
