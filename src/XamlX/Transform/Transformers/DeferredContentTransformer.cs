using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

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
            var deferredAttr =
                pa.Property.CustomAttributes.FirstOrDefault(ca => deferredAttrs.Any(da => da.Equals(ca.Type)));
            if (deferredAttr == null)
                return node;

            if (pa.Values.Count != 1)
                throw new XamlTransformException("Property with deferred content can have only one value", node);
            var contentNode = pa.Values[0];

            if (!
                (contentNode is XamlValueWithManipulationNode manipulation
                 && manipulation.Manipulation is XamlObjectInitializationNode))
                throw new XamlTransformException(
                    "Unable to find the object initialization node inside deferred content, " +
                    "this shouldn't happen in default Xaml configuration, probably some AST transformer have broken the structure",
                    node);
            manipulation.Value = new XamlDeferredContentInitializeIntermediateRootNode(manipulation.Value);

            
            // Find the type param for the customizer.
            // In Avalonia that's TemplateContentAttribute::TemplateResult property
            // It is used to return a somewhat strongly typed results from templates
            IXamlType? typeParam = context.Configuration.TypeMappings
                .DeferredContentExecutorCustomizationDefaultTypeParameter;
            var customizationTypeParamPropertyNames = context.Configuration.TypeMappings
                .DeferredContentExecutorCustomizationTypeParameterDeferredContentAttributePropertyNames;
            if (customizationTypeParamPropertyNames.Any())
            {
                foreach (var propertyName in customizationTypeParamPropertyNames)
                {
                    if (deferredAttr.Properties.TryGetValue(propertyName, out var value))
                    {
                        typeParam = (IXamlType?)value;
                        break;
                    }
                }
            }
            pa.Values[0] =
                new XamlDeferredContentNode(pa.Values[0], typeParam, context.Configuration);
            return node;
        }
    }
}
