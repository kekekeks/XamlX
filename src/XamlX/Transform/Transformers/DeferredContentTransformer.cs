using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class DeferredContentTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlPropertyAssignmentNode pa))
                return node;
            var deferredAttrs = context.Configuration.TypeMappings.DeferredContentPropertyAttributes;
            if (deferredAttrs.Count == 0)
                return node;
            if (!pa.Property.CustomAttributes.Any(ca => deferredAttrs.Any(da => da.Equals(ca.Type))))
                return node;

            if (pa.Values.Count != 1)
                throw new XamlParseException("Property with deferred content can have only one value", node);
            var contentNode = pa.Values[0];

            if (!
                (contentNode is XamlValueWithManipulationNode manipulation
                 && manipulation.Manipulation is XamlObjectInitializationNode))
                throw new XamlParseException(
                    "Unable to find the object initialization node inside deferred content, " +
                    "this shouldn't happen in default Xaml configuration, probably some AST transformer have broken the structure",
                    node);
            manipulation.Value = new XamlDeferredContentInitializeIntermediateRootNode(manipulation.Value);
            
            pa.Values[0] =
                new XamlDeferredContentNode(pa.Values[0], context.Configuration);
            return node;
        }
    }
}
