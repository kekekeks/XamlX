using System;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    /// <summary>
    /// For <see cref="XamlAstObjectNode">AST object nodes</see>, this transformer will collect all direct children
    /// that are <see cref="IXamlAstValueNode">AST value nodes</see> and wrap them into an <see cref="XamlAstXamlPropertyValueNode">
    /// AST property value node</see>, which will be appended to the transformed node's children.
    /// The property value node will refer to the target XAML type's content property.
    /// </summary>
#if !XAMLX_INTERNAL
    public
#endif
    class ResolveContentPropertyTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni)
            {
                var contentProperty = context.Configuration.FindContentProperty(ni.Type.GetClrType());
                XamlAstXamlPropertyValueNode propertyNode = null;

                if (ni.Children.OfType<IXamlAstValueNode>().Any())
                {
                    if (contentProperty is null)
                    {
                        var adders = XamlTransformHelpers.FindPossibleAdders(context, ni.Type.GetClrType());
                        if (adders.Count is 0)
                        {
                            WhitespaceNormalization.RemoveWhitespaceNodes(ni.Children);
                            var firstValueChild = ni.Children.OfType<IXamlAstValueNode>().FirstOrDefault();
                            if (firstValueChild is not null)
                            {
                                throw new XamlTransformException(
                                    $"No Content property or any Add methods found for type {ni.Type.GetClrType().GetFqn()}",
                                    firstValueChild);
                            }
                        }
                        else
                        {
                            propertyNode = new XamlAstXamlPropertyValueNode(ni, new XamlAstClrProperty(ni,
                                "Content",
                                ni.Type.GetClrType(),
                                null,
                                adders.Select(a => new XamlDirectCallPropertySetter(a)
                                {
                                    BinderParameters = { AllowMultiple = true }
                                })),
                                Array.Empty<IXamlAstValueNode>(),
                                false);
                        }
                    }
                    else
                    {
                        propertyNode = new XamlAstXamlPropertyValueNode(ni,
                            new XamlAstClrProperty(ni, contentProperty, context.Configuration),
                            Array.Empty<IXamlAstValueNode>(), false);
                    }

                    for (var c = ni.Children.Count - 1; c >= 0; c--)
                    {
                        var child = ni.Children[c];
                        if (child is IXamlAstValueNode valueNode)
                        {
                            propertyNode.Values.Insert(0, valueNode);
                            ni.Children.RemoveAt(c);
                        }
                    }
                }

                if (propertyNode != null)
                    ni.Children.Add(propertyNode);

            }

            return node;
        }
    }
}
