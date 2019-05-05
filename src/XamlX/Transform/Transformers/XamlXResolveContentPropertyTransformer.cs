using System;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlXResolveContentPropertyTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstObjectNode ni)
            {
                XamlXAstXamlPropertyValueNode propertyNode = null;
                
                for (var c = ni.Children.Count - 1; c >= 0; c--)
                {
                    var child = ni.Children[c];
                    if (child is IXamlXAstValueNode valueNode)
                    {
                        if (propertyNode == null)
                        {
                            var contentProperty = context.Configuration.FindContentProperty(ni.Type.GetClrType());
                            if (contentProperty != null)
                                propertyNode = new XamlXAstXamlPropertyValueNode(ni,
                                    new XamlXAstClrProperty(ni, contentProperty),
                                    Array.Empty<IXamlXAstValueNode>());
                            else
                            {
                                var adders = XamlXTransformHelpers.FindPossibleAdders(context, ni.Type.GetClrType());
                                if (adders.Count == 0)
                                    throw new XamlXParseException(
                                        $"No Content property or any Add methods found for type {ni.Type.GetClrType().GetFqn()}",
                                        child);
                                propertyNode = new XamlXAstXamlPropertyValueNode(ni, new XamlXAstClrProperty(ni,
                                        "Content", ni.Type.GetClrType(), null,
                                        adders.Select(a => new XamlXDirectCallPropertySetter(a))),
                                    Array.Empty<IXamlXAstValueNode>());
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
