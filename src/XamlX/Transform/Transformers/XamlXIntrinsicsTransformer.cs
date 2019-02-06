using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXIntrinsicsTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstObjectNode ni 
                && ni.Type is XamlXAstXmlTypeReference xml
                && xml.XmlNamespace == XamlNamespaces.Xaml2006)
            {
                XamlXAstTextNode ResolveArgumentOrValue(string extension, string name)
                {
                    IXamlXAstNode value = null;
                    
                    if (ni.Arguments.Count == 1 && ni.Children.Count == 0)
                        value = ni.Arguments[0];
                    else if (ni.Arguments.Count == 0 && ni.Children.Count == 1
                                                     && ni.Children[0] is XamlXAstXamlPropertyValueNode pnode
                                                     && pnode.Property is XamlXAstNamePropertyReference pref
                                                     && pref.Name == name
                                                     && pnode.Values.Count == 1) 
                        value = pnode.Values[0];

                    if(value == null)
                        return (XamlXAstTextNode) context.ParseError(
                            $"{extension} extension should take exactly one constructor parameter without any content OR {name} property",
                            node);

                    if (!(value is XamlXAstTextNode textNode))
                        return (XamlXAstTextNode) context.ParseError("x:Type parameter should be a text node", value, node);
                    return textNode;
                }
                
                if (xml.Name == "Null")
                    return new XamlXNullExtensionNode(node);
                if (xml.Name == "Type")
                {
                    var textNode = ResolveArgumentOrValue("x:Type", "TypeName");
                    if (textNode == null)
                        return null;
                        

                    var typeRefText = textNode.Text.Trim();
                    var pair = typeRefText.Split(new[] {':'}, 2);
                    if (pair.Length == 1)
                        pair = new[] {"", pair[0]};

                    if (!context.NamespaceAliases.TryGetValue(pair[0].Trim(), out var resolvedNs))
                        return context.ParseError($"Unable to resolve namespace {pair[0]}", textNode, node);

                    return new XamlXTypeExtensionNode(node,
                        new XamlXAstXmlTypeReference(textNode, resolvedNs, pair[1], xml.GenericArguments),
                        context.Configuration.TypeSystem.FindType("System.Type"));
                }

                if (xml.Name == "Static")
                {
                    var textNode = ResolveArgumentOrValue("x:Static", "Member");
                    if (textNode == null)
                        return null;
                    var nsp = textNode.Text.Trim().Split(new[] {':'}, 2);
                    string ns, typeAndMember;
                    if (nsp.Length == 1)
                    {
                        ns = "";
                        typeAndMember = nsp[0];
                    }
                    else
                    {
                        ns = nsp[0];
                        typeAndMember = nsp[1];
                    }

                    var tmpair = typeAndMember.Split(new[] {'.'}, 2);
                    if (tmpair.Length != 2)
                        return context.ParseError($"Unable to parse {tmpair} as 'type.member'", textNode, ni);
                    
                    if (!context.NamespaceAliases.TryGetValue(ns, out var resolvedNs))
                        return context.ParseError($"Unable to resolve namespace {ns}", textNode, node);
                    
                    return new XamlXStaticExtensionNode(ni,
                        new XamlXAstXmlTypeReference(ni, resolvedNs, tmpair[0], xml.GenericArguments),
                        tmpair[1]);
                }
            }

            return node;
        }
    }
}