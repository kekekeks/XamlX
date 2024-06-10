using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlIntrinsicsTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni 
                && ni.Type is XamlAstXmlTypeReference xml
                && xml.XmlNamespace == XamlNamespaces.Xaml2006)
            {
                XamlAstTextNode ResolveArgumentOrValue(string extension, string name)
                {
                    IXamlAstNode? value = null;
                    
                    if (ni.Arguments.Count == 1 && ni.Children.Count == 0)
                        value = ni.Arguments[0];
                    else if (ni.Arguments.Count == 0 && ni.Children.Count == 1
                                                     && ni.Children[0] is XamlAstXamlPropertyValueNode pnode
                                                     && pnode.Property is XamlAstNamePropertyReference pref
                                                     && pref.Name == name
                                                     && pnode.Values.Count == 1) 
                        value = pnode.Values[0];

                    if(value == null)
                        throw new XamlTransformException(
                            $"{extension} extension should take exactly one constructor parameter without any content OR {name} property",
                            node);

                    if (!(value is XamlAstTextNode textNode))
                        throw new XamlTransformException("x:Type parameter should be a text node", node);
                    return textNode;
                }

                if (xml.Name == "Null")
                    return new XamlNullExtensionNode(node);
                if (xml.Name == "True")
                    return new XamlConstantNode(node, context.Configuration.WellKnownTypes.Boolean, true);
                if (xml.Name == "False")
                    return new XamlConstantNode(node, context.Configuration.WellKnownTypes.Boolean, false);
                if (xml.Name == "Type")
                {
                    var textNode = ResolveArgumentOrValue("x:Type", "TypeName");
                    var typeRefText = textNode.Text.Trim();
                    var pair = typeRefText.Split(new[] {':'}, 2);
                    if (pair.Length == 1)
                        pair = new[] {"", pair[0]};

                    if (!context.NamespaceAliases.TryGetValue(pair[0].Trim(), out var resolvedNs))
                        return context.ReportTransformError($"Unable to resolve namespace {pair[0]}", textNode, node);

                    return new XamlTypeExtensionNode(node,
                        new XamlAstXmlTypeReference(textNode, resolvedNs, pair[1], xml.GenericArguments),
                        context.Configuration.TypeSystem.GetType("System.Type"));
                }

                if (xml.Name == "Static")
                {
                    var textNode = ResolveArgumentOrValue("x:Static", "Member");
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
                        throw new XamlTransformException($"Unable to parse {typeAndMember} as 'type.member'", textNode);
                    
                    if (!context.NamespaceAliases.TryGetValue(ns, out var resolvedNs))
                        throw new XamlTransformException($"Unable to resolve namespace {ns}", textNode);
                    
                    return new XamlStaticExtensionNode(ni,
                        new XamlAstXmlTypeReference(ni, resolvedNs, tmpair[0], xml.GenericArguments),
                        tmpair[1]);
                }
            }

            return node;
        }
    }
}
