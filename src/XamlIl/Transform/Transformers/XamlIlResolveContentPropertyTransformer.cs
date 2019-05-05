using System;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlResolveContentPropertyTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode ni)
            {
                XamlIlAstXamlPropertyValueNode propertyNode = null;
                
                for (var c = ni.Children.Count - 1; c >= 0; c--)
                {
                    var child = ni.Children[c];
                    if (child is IXamlIlAstValueNode valueNode)
                    {
                        if (propertyNode == null)
                        {
                            var contentProperty = context.Configuration.FindContentProperty(ni.Type.GetClrType());
                            if (contentProperty != null)
                                propertyNode = new XamlIlAstXamlPropertyValueNode(ni,
                                    new XamlIlAstClrProperty(ni, contentProperty),
                                    Array.Empty<IXamlIlAstValueNode>());
                            else
                            {
                                var adders = XamlIlTransformHelpers.FindPossibleAdders(context, ni.Type.GetClrType());
                                if (adders.Count == 0)
                                    throw new XamlIlParseException(
                                        $"No Content property or any Add methods found for type {ni.Type.GetClrType().GetFqn()}",
                                        child);
                                propertyNode = new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstClrProperty(ni,
                                        "Content", ni.Type.GetClrType(), null,
                                        adders.Select(a => new XamlIlDirectCallPropertySetter(a))),
                                    Array.Empty<IXamlIlAstValueNode>());
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
