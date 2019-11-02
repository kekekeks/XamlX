using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    abstract class XamlCompiler<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        protected readonly XamlTransformerConfiguration _configuration;
        protected readonly XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> _emitMappings;

        public List<IXamlAstTransformer> Transformers { get; } = new List<IXamlAstTransformer>();
        public List<IXamlAstTransformer> SimplificationTransformers { get; } = new List<IXamlAstTransformer>();
        
        public XamlCompiler(XamlTransformerConfiguration configuration,
            XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> emitMappings,
            bool fillWithDefaults)
        {
            _configuration = configuration;
            _emitMappings = emitMappings;
            if (fillWithDefaults)
            {
                Transformers = new List<IXamlAstTransformer>
                {
                    new XamlKnownDirectivesTransformer(),
                    new XamlIntrinsicsTransformer(),
                    new XamlXArgumentsTransformer(),
                    new XamlTypeReferenceResolver(),
                    new XamlMarkupExtensionTransformer(),
                    new XamlPropertyReferenceResolver(),
                    new XamlContentConvertTransformer(),
                    new XamlResolveContentPropertyTransformer(),
                    new XamlResolvePropertyValueAddersTransformer(),
                    new XamlConvertPropertyValuesToAssignmentsTransformer()
                };
                SimplificationTransformers = new List<IXamlAstTransformer>
                {
                    new XamlFlattenTransformer()
                };
            }
        }

        public XamlAstTransformationContext CreateTransformationContext(XamlDocument doc, bool strict)
            => new XamlAstTransformationContext(_configuration, doc.NamespaceAliases, strict);
        
        public void Transform(XamlDocument doc, bool strict = true)
        {
            var ctx = CreateTransformationContext(doc, strict);

            var root = doc.Root;
            ctx.RootObject = new XamlRootObjectNode((XamlAstObjectNode)root);
            foreach (var transformer in Transformers)
            {
                ctx.VisitChildren(ctx.RootObject, transformer);
                root = ctx.Visit(root, transformer);
            }

            foreach (var simplifier in SimplificationTransformers)
                root = ctx.Visit(root, simplifier);

            doc.Root = root;
        }

        protected abstract XamlXEmitContext<TBackendEmitter, TEmitResult> InitCodeGen(
            IFileSource file,
            Func<string, IXamlType, IXamlTypeBuilder<TBackendEmitter>> createSubType,
            TBackendEmitter codeGen, XamlRuntimeContext<TBackendEmitter, TEmitResult> context, bool needContextLocal);
    }


#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstTransformer
    {
        IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node);
    }
}
