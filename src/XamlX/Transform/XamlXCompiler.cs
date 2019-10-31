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
    class XamlCompiler
    {
        protected readonly XamlTransformerConfiguration _configuration;
        public List<IXamlAstTransformer> Transformers { get; } = new List<IXamlAstTransformer>();
        public List<IXamlAstTransformer> SimplificationTransformers { get; } = new List<IXamlAstTransformer>();
        
        public XamlCompiler(XamlTransformerConfiguration configuration, bool fillWithDefaults)
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
    }


#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstTransformer
    {
        IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node);
    }
}
