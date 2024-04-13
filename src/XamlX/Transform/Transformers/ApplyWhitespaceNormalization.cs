using System;
using System.Collections.Generic;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    // See: https://docs.microsoft.com/en-us/dotnet/desktop/xaml-services/white-space-processing
    // Must be applied after content has been transformed to a XamlAstXamlPropertyValueNode,
    // and after ResolvePropertyValueAddersTransformer has resolved the Add methods for collection properties
#if !XAMLX_INTERNAL
    public
#endif
    class ApplyWhitespaceNormalization : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXamlPropertyValueNode propertyNode)
            {
                var childNodes = propertyNode.Values;

                var property = propertyNode.Property.GetClrProperty();
      
                WhitespaceNormalization.Apply(childNodes, context.Configuration);              

                if (!WantsWhitespaceOnlyElements(context.Configuration, property, childNodes))
                {
                    WhitespaceNormalization.RemoveWhitespaceNodes(childNodes);
                }
            }

            return node;
        }

        private static bool WantsWhitespaceOnlyElements(TransformerConfiguration config,
            XamlAstClrProperty property, IList<IXamlAstValueNode> childNodes)
        {
            var wellKnownTypes = config.WellKnownTypes;

            // A collection-like property will only receive whitespace-only nodes if the
            // property type can be deduced, and that type is annotated as whitespace significant
            if (property.Getter != null && config.IsWhitespaceSignificantCollection(property.Getter.ReturnType))
            {
                return true;
            }

            foreach (var setter in property.Setters)
            {
                // Skip any dictionary-like setters
                if (setter.Parameters.Count != 1)
                {
                    continue;
                }

                var parameterType = setter.Parameters[0];

                if (!setter.BinderParameters.AllowMultiple)
                {
                    if(childNodes.Count > 1)
                    {
                        return false;
                    }

                    // If the property can accept a scalar string, it'll get whitespace nodes by default
                    if (parameterType.Equals(wellKnownTypes.String) || parameterType.Equals(wellKnownTypes.Object))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
