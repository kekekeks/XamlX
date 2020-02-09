using System;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class ResolveContentPropertyTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni)
            {
                XamlAstXamlPropertyValueNode propertyNode = null;
                
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
                                    Array.Empty<IXamlAstValueNode>());
                            else
                            {
                                var adders = XamlTransformHelpers.FindPossibleAdders(context, ni.Type.GetClrType());
                                if (adders.Count == 0)
                                    throw new XamlParseException(
                                        $"No Content property or any Add methods found for type {ni.Type.GetClrType().GetFqn()}",
                                        child);
                                propertyNode = new XamlAstXamlPropertyValueNode(ni, new XamlAstClrProperty(ni,
                                        "Content", ni.Type.GetClrType(), null,
                                        adders.Select(a => new XamlDirectCallPropertySetter(a)
                                        {
                                            BinderParameters = {AllowMultiple = true}
                                        })),
                                    Array.Empty<IXamlAstValueNode>());
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
