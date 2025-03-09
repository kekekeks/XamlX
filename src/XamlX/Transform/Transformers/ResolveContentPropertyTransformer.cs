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
                XamlAstXamlPropertyValueNode? propertyNode = null;

                for (var c = ni.Children.Count - 1; c >= 0; c--)
                {
                    var child = ni.Children[c];
                    if (child is IXamlAstValueNode valueNode)
                    {
                        if (propertyNode == null)
                        {
                            var contentProperty = context.Configuration.FindContentProperty(ni.Type.GetClrType());
                            if (contentProperty != null)
                                propertyNode = new XamlAstXamlPropertyValueNode(ni,
                                    new XamlAstClrProperty(ni, contentProperty, context.Configuration),
                                    Array.Empty<IXamlAstValueNode>(), false);
                            else
                            {
                                var adders = XamlTransformHelpers.FindPossibleAdders(context, ni.Type.GetClrType());
                                if (adders.Count == 0)
                                {
                                    // If there's no content property, strip all whitespace-only nodes and continue
                                    WhitespaceNormalization.RemoveWhitespaceNodes(ni.Children);
                                    if (!ni.Children.Contains(child))
                                    {
                                        continue;
                                    }

                                    throw new XamlTransformException(
                                        $"No Content property or any Add methods found for type {ni.Type.GetClrType().GetFqn()}",
                                        child);
                                }

                                propertyNode = new XamlAstXamlPropertyValueNode(ni, new XamlAstClrProperty(ni,
                                        "Content", ni.Type.GetClrType(), null,
                                        adders.Select(a => new XamlDirectCallPropertySetter(a)
                                        {
                                            BinderParameters = {AllowMultiple = true}
                                        }), null),
                                    Array.Empty<IXamlAstValueNode>(),
                                    false);
                            }
                        }
                        // We are going in reverse order, so insert at the beginning
                        propertyNode.Values.Insert(0, valueNode);
                        ni.Children.RemoveAt(c);
                    }
                }

                if (propertyNode != null)
                    ni.Children.Add(propertyNode);

            }

            return node;
        }
    }
}
