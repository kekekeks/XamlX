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
                XamlIlAstTextNode ResolveArgumentOrValue(string extension, string name)
                {
                    IXamlIlAstNode value = null;
                    
                    if (ni.Arguments.Count == 1 && ni.Children.Count == 0)
                        value = ni.Arguments[0];
                    else if (ni.Arguments.Count == 0 && ni.Children.Count == 1
                                                     && ni.Children[0] is XamlIlAstXamlPropertyValueNode pnode
                                                     && pnode.Property is XamlIlAstNamePropertyReference pref
                                                     && pref.Name == name
                                                     && pnode.Values.Count == 1) 
                        value = pnode.Values[0];

                    if(value == null)
                        return (XamlIlAstTextNode) context.ParseError(
                            $"{extension} extension should take exactly one constructor parameter without any content OR {name} property",
                            node);

                    if (!(value is XamlIlAstTextNode textNode))
                        return (XamlIlAstTextNode) context.ParseError("x:Type parameter should be a text node", value, node);
                    return textNode;
                }
                
                if (xml.Name == "Null")
                    return new XamlIlNullExtensionNode(node);
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

                    return new XamlIlTypeExtensionNode(node,
                        new XamlIlAstXmlTypeReference(textNode, resolvedNs, pair[1], xml.GenericArguments),
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
                    
                    return new XamlIlStaticExtensionNode(ni,
                        new XamlIlAstXmlTypeReference(ni, resolvedNs, tmpair[0], xml.GenericArguments),
                        tmpair[1]);
                }
            }

            return node;
        }
    }
}