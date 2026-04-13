using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlIntrinsicsTransformer : IXamlAstTransformer
    {
        [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypeInCoreAssembly)]
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni 
                && ni.Type is XamlAstXmlTypeReference xml
                && xml.XmlNamespace == XamlNamespaces.Xaml2006)
            {
                switch (xml.Name)
                {
                    case "Null":
                        return new XamlNullExtensionNode(node);
                    case "True":
                        return new XamlConstantNode(node, context.Configuration.WellKnownTypes.Boolean, true);
                    case "False":
                        return new XamlConstantNode(node, context.Configuration.WellKnownTypes.Boolean, false);
                    case "Type":
                    {
                        var textNode = ResolveArgumentOrValue("x:Type", "TypeName");
                        var typeRefText = textNode.Text.Trim();

                        var typeReference = ResolveTypeExpression("x:Type", typeRefText, textNode);

                        return new XamlTypeExtensionNode(node,
                            typeReference,
                            context.Configuration.TypeSystem.GetType("System.Type"));
                    }
                    case "Static":
                    {
                        var textNode = ResolveArgumentOrValue("x:Static", "Member");
                        var typeRefText = textNode.Text.Trim();

                        var tmpair = typeRefText.Split(['.'], 2);

                        if (tmpair.Length != 2)
                            throw new XamlTransformException($"Unable to parse {typeRefText} as 'type.member'", textNode);

                        var typeReference = ResolveTypeExpression("x:Static", tmpair[0], textNode);

                        return new XamlStaticExtensionNode(ni,
                            typeReference,
                            tmpair[1]);
                    }
                }
            }

            return node;


            XamlAstTextNode ResolveArgumentOrValue(string extension, string name)
            {
                IXamlAstNode? value = null;

                if (ni.Arguments.Count == 1 && ni.Children.Count == 0)
                    value = ni.Arguments[0];
                else if (ni.Arguments.Count == 0
                         && ni.Children.Count == 1
                         && ni.Children[0] is XamlAstXamlPropertyValueNode pNode
                         && pNode.Property is XamlAstNamePropertyReference pRef
                         && pRef.Name == name
                         && pNode.Values.Count == 1)
                    value = pNode.Values[0];

                if (value == null)
                    throw new XamlTransformException(
                        $"{extension} extension should take exactly one constructor parameter without any content OR {name} property",
                        node);

                if (!(value is XamlAstTextNode textNode))
                    throw new XamlTransformException("x:Type parameter should be a text node", node);
                return textNode;
            }

            XamlAstXmlTypeReference ResolveTypeExpression(string extensionName, string typeExpression,
                XamlAstTextNode textNode)
            {
                var hasInlineGenericSyntax = typeExpression.IndexOfAny(['(', ')']) != -1;

                if (xml.GenericArguments.Count > 0 && hasInlineGenericSyntax)
                    throw new XamlTransformException(
                        $"{extensionName} generic arguments cannot be specified both inline and in x:TypeArguments",
                        textNode);

                if (hasInlineGenericSyntax)
                {
                    try
                    {
                        var tree = CommaSeparatedParenthesesTreeParser.Parse(typeExpression);
                        if (tree.Count != 1)
                            throw new XamlTransformException(
                                $"{extensionName} should contain exactly one type expression",
                                textNode);

                        return ConvertNode(tree[0]);
                    }
                    catch (CommaSeparatedParenthesesTreeParser.ParseException e)
                    {
                        throw new XamlTransformException($"Unable to parse {extensionName}: {e.Message}", textNode, e);
                    }
                }

                var typeRef = ResolveTypeName(typeExpression, textNode, null);

                if (xml.GenericArguments.Count > 0)
                    typeRef.GenericArguments = xml.GenericArguments;

                return typeRef;

                XamlAstXmlTypeReference ConvertNode(CommaSeparatedParenthesesTreeParser.Node parsedNode)
                {
                    var parsedValue = parsedNode.Value;
                    if (parsedValue == null || string.IsNullOrWhiteSpace(parsedValue))
                        throw new XamlTransformException($"{extensionName} contains an empty type name", textNode);

                    var trimmed = parsedValue.Trim();

                    var genericArguments = parsedNode.Children.Select(ConvertNode).ToList();
                    var typeReference = ResolveTypeName(trimmed, textNode, genericArguments);

                    return typeReference;
                }


                XamlAstXmlTypeReference ResolveTypeName(string typeName, XamlAstTextNode textNodeInner, List<XamlAstXmlTypeReference>? genericArguments)
                {
                    var isNullable = typeName.EndsWith("?");
                    if (isNullable)
                        typeName = typeName.Substring(0, typeName.Length - 1);

                    if (typeName.Contains("?"))
                        throw new XamlTransformException(
                            $"Type {typeName}? is cannot contain multiple nullable indicators ('?')",
                            textNodeInner);

                    var pair = typeName.Split([':'], 2);
                    if (pair.Length == 1)
                        pair = ["", pair[0]];

                    if (!context.NamespaceAliases.TryGetValue(pair[0].Trim(), out var resolvedNs))
                        throw new XamlTransformException($"Unable to resolve namespace {pair[0]}", textNodeInner);

                    var typeReference = new XamlAstXmlTypeReference(textNodeInner, resolvedNs, pair[1].Trim());

                    if (genericArguments is not null)
                        typeReference.GenericArguments = genericArguments;

                    if (isNullable)
                    {
                        var resolved = TypeReferenceResolver.ResolveType(context, typeReference);
                        if (resolved.Type.IsValueType)
                            return new XamlAstXmlTypeReference(textNodeInner, XamlNamespaces.Xaml2006, "Nullable`1",
                                [typeReference]);
                    }

                    return typeReference;
                }
            }
        }
    }
}