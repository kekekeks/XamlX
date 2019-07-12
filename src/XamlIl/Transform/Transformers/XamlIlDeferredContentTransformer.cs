using System.Linq;
using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlDeferredContentTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (!(node is XamlIlPropertyAssignmentNode pa))
                return node;
            var deferredAttrs = context.Configuration.TypeMappings.DeferredContentPropertyAttributes;
            if (deferredAttrs.Count == 0)
                return node;
            if (!pa.Property.CustomAttributes.Any(ca => deferredAttrs.Any(da => da.Equals(ca.Type))))
                return node;

            if (pa.Values.Count != 1)
                throw new XamlIlParseException("Property with deferred content can have only one value", node);
            var contentNode = pa.Values[0];

            if (!
                (contentNode is XamlIlValueWithManipulationNode manipulation
                 && manipulation.Manipulation is XamlIlObjectInitializationNode))
                throw new XamlIlParseException(
                    "Unable to find the object initialization node inside deferred content, " +
                    "this shouldn't happen in default XamlIl configuration, probably some AST transformer have broken the structure",
                    node);
            manipulation.Value = new XamlIlDeferredContentInitializeIntermediateRootNode(manipulation.Value);
            
            pa.Values[0] =
                new XamlIlDeferredContentNode(pa.Values[0], context.Configuration);
            return node;
        }
    }
}
