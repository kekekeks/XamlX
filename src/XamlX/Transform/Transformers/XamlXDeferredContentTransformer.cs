using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXDeferredContentTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (!(node is XamlXPropertyAssignmentNode pa))
                return node;
            var deferredAttrs = context.Configuration.TypeMappings.DeferredContentPropertyAttributes;
            if (deferredAttrs.Count == 0)
                return node;
            if (!pa.Property.CustomAttributes.Any(ca => deferredAttrs.Any(da => da.Equals(ca.Type))))
                return node;

            if (pa.Values.Count != 1)
                throw new XamlXParseException("Property with deferred content can have only one value", node);
            var contentNode = pa.Values[0];

            if (!
                (contentNode is XamlXValueWithManipulationNode manipulation
                 && manipulation.Manipulation is XamlXObjectInitializationNode))
                throw new XamlXParseException(
                    "Unable to find the object initialization node inside deferred content, " +
                    "this shouldn't happen in default XamlX configuration, probably some AST transformer have broken the structure",
                    node);
            manipulation.Value = new XamlXDeferredContentInitializeIntermediateRootNode(manipulation.Value);
            
            pa.Values[0] =
                new XamlXDeferredContentNode(pa.Values[0], context.Configuration);
            return node;
        }
    }
}
