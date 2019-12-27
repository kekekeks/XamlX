using System;
using System.Collections.Generic;
using System.Reflection.Emit;
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
    abstract class XamlCompiler<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        protected readonly TransformerConfiguration _configuration;
        protected readonly XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> _emitMappings;

        public List<IXamlAstTransformer> Transformers { get; } = new List<IXamlAstTransformer>();
        public List<IXamlAstTransformer> SimplificationTransformers { get; } = new List<IXamlAstTransformer>();

        public List<object> Emitters { get; } = new List<object>();

        public XamlCompiler(TransformerConfiguration configuration,
            XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> emitMappings,
            bool fillWithDefaults)
        {
            _configuration = configuration;
            _emitMappings = emitMappings;
            if (fillWithDefaults)
            {
                Transformers = new List<IXamlAstTransformer>
                {
                    new KnownDirectivesTransformer(),
                    new XamlIntrinsicsTransformer(),
                    new XArgumentsTransformer(),
                    new TypeReferenceResolver(),
                    new MarkupExtensionTransformer(),
                    new PropertyReferenceResolver(),
                    new ContentConvertTransformer(),
                    new ResolveContentPropertyTransformer(),
                    new ResolvePropertyValueAddersTransformer(),
                    new ConvertPropertyValuesToAssignmentsTransformer(),
                    new ConstructableObjectTransformer()
                };
                SimplificationTransformers = new List<IXamlAstTransformer>
                {
                    new FlattenAstTransformer()
                };
            }
        }

        public AstTransformationContext CreateTransformationContext(XamlDocument doc, bool strict)
            => new AstTransformationContext(_configuration, doc.NamespaceAliases, strict);
        
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

        protected abstract XamlEmitContext<TBackendEmitter, TEmitResult> InitCodeGen(
            IFileSource file,
            Func<string, IXamlType, IXamlTypeBuilder<TBackendEmitter>> createSubType,
            TBackendEmitter codeGen, XamlRuntimeContext<TBackendEmitter, TEmitResult> context, bool needContextLocal);
    }
}
