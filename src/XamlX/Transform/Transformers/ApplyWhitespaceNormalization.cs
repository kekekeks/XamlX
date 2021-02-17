using System;
using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    // See: https://docs.microsoft.com/en-us/dotnet/desktop/xaml-services/white-space-processing
    // Must be applied after content has been transformed to a XamlAstXamlPropertyValueNode
    public class ApplyWhitespaceNormalization : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXamlPropertyValueNode propertyNode)
            {
                var childeNodes = propertyNode.Values;
                WhitespaceNormalization.Apply(
                    childeNodes,
                    context.Configuration
                );

                // This heuristic applies to property types that are collections
                var property = propertyNode.Property.GetClrProperty();
                var propertyType = property.PropertyType;
                if (context.Configuration.WellKnownTypes.IList.IsAssignableFrom(propertyType)
                    || context.Configuration.WellKnownTypes.IListOfT.IsAssignableFrom(propertyType)
                    || context.Configuration.WellKnownTypes.IEnumerable.IsAssignableFrom(propertyType)
                    || context.Configuration.WellKnownTypes.IEnumerableT.IsAssignableFrom(propertyType))
                {
                    var significantWhitespaceCollection =
                        context.Configuration.IsWhitespaceSignificantCollection(propertyType);
                    if (!significantWhitespaceCollection)
                    {
                        WhitespaceNormalization.RemoveWhitespaceNodes(childeNodes);
                    }
                }
            }

            return node;
        }
    }
}