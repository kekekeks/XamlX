﻿using System;
using System.Collections.Generic;
using System.Text;
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

        public void Compile(XamlDocument doc, IXamlTypeBuilder<TBackendEmitter> typeBuilder, IXamlType contextType,
            string populateMethodName, string createMethodName, string namespaceInfoClassName,
            string baseUri, IFileSource fileSource)
        {
            var rootGrp = (XamlValueWithManipulationNode)doc.Root;
            Compile(doc, contextType,
                DefinePopulateMethod(typeBuilder, doc, populateMethodName, XamlVisibility.Public),
                createMethodName == null ?
                    null :
                    DefineBuildMethod(typeBuilder, doc, createMethodName, XamlVisibility.Public),
                _configuration.TypeMappings.XmlNamespaceInfoProvider == null ?
                    null :
                    typeBuilder.DefineSubType(_configuration.WellKnownTypes.Object,
                        namespaceInfoClassName, XamlVisibility.Private),
                (name, bt) => typeBuilder.DefineSubType(bt, name, XamlVisibility.Private),
                (s, returnType, parameters) => typeBuilder.DefineDelegateSubType(s, XamlVisibility.Private, returnType, parameters),
                baseUri, fileSource);
        }

        public void Compile(XamlDocument doc, IXamlType contextType,
            IXamlMethodBuilder<TBackendEmitter> populateMethod, IXamlMethodBuilder<TBackendEmitter> buildMethod,
            IXamlTypeBuilder<TBackendEmitter> namespaceInfoBuilder,
            Func<string, IXamlType, IXamlTypeBuilder<TBackendEmitter>> createClosure,
            Func<string, IXamlType, IEnumerable<IXamlType>, IXamlTypeBuilder<TBackendEmitter>> createDelegateType,
            string baseUri, IFileSource fileSource)
        {
            var rootGrp = (XamlValueWithManipulationNode)doc.Root;
            var rootType = rootGrp.Type.GetClrType();
            var context = CreateRuntimeContext(doc, contextType, namespaceInfoBuilder, baseUri, rootType);

            CompilePopulate(fileSource, rootGrp.Manipulation, createClosure, createDelegateType, populateMethod.Generator, context);

            if (buildMethod != null)
            {
                CompileBuild(fileSource, rootGrp.Value, null, createDelegateType, buildMethod.Generator, context, populateMethod);
            }

            namespaceInfoBuilder?.CreateType();
        }

        protected abstract void CompilePopulate(IFileSource fileSource, IXamlAstManipulationNode manipulation,
            Func<string, IXamlType, IXamlTypeBuilder<TBackendEmitter>> createSubType,
            Func<string, IXamlType, IEnumerable<IXamlType>, IXamlTypeBuilder<TBackendEmitter>> DefineDelegateSubType,
            TBackendEmitter codeGen, XamlRuntimeContext<TBackendEmitter, TEmitResult> context);

        protected abstract void CompileBuild(
            IFileSource fileSource,
            IXamlAstValueNode rootInstance, Func<string, IXamlType, IXamlTypeBuilder<TBackendEmitter>> createSubType,
            Func<string, IXamlType, IEnumerable<IXamlType>, IXamlTypeBuilder<TBackendEmitter>> createDelegateType,
            TBackendEmitter codeGen, XamlRuntimeContext<TBackendEmitter, TEmitResult> context,
            IXamlMethod compiledPopulate);

        protected abstract XamlRuntimeContext<TBackendEmitter, TEmitResult> CreateRuntimeContext(
            XamlDocument doc, IXamlType contextType,
            IXamlTypeBuilder<TBackendEmitter> namespaceInfoBuilder, string baseUri, IXamlType rootType);
    }
}
