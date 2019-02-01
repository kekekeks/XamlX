using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlIntrinsicsTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstNewInstanceNode ni 
                && ni.Type is XamlIlAstXmlTypeReference xml
                && xml.XmlNamespace == XamlNamespaces.Xaml2006)
            {
                if (xml.Name == "Null")
                    return new XamlIlNullDirectiveNode(node);
                if (xml.Name == "Type")
                {
                    IXamlIlAstNode value = null;
                    
                    if (ni.Arguments.Count == 1 && ni.Children.Count == 0)
                        value = ni.Arguments[0];
                    else if (ni.Arguments.Count == 0 && ni.Children.Count == 1
                             && ni.Children[0] is XamlIlAstXamlPropertyValueNode pnode
                             && pnode.Values.Count == 1) 
                        value = pnode.Values[0];

                    if(value == null)
                        return context.ParseError(
                            "x:Type extension should take exactly one constructor parameter without any content OR TypeName property",
                            node);

                    if (!(value is XamlIlAstTextNode textNode))
                        return context.ParseError("x:Type parameter should be a text node", value, node);

                    var typeRefText = textNode.Text.Trim();
                    var pair = typeRefText.Split(new[] {':'}, 2);
                    if (pair.Length == 1)
                        pair = new[] {"", pair[0]};

                    if (!context.NamespaceAliases.TryGetValue(pair[0].Trim(), out var resolvedNs))
                        return context.ParseError($"Unable to resolve namespace {pair[0]}", textNode, node);

                    return new XamlIlTypeDirectiveNode(node,
                        new XamlIlAstXmlTypeReference(textNode, resolvedNs, pair[1], xml.GenericArguments),
                        context.Configuration.TypeSystem.FindType("System.Type"));

                }
            }

            return node;
        }
    }
}