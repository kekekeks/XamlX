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
                    var hasInlineGenericSyntax = typeRefText.IndexOfAny(new[] { '(', ')' }) != -1;

                    if (xml.GenericArguments.Count > 0 && hasInlineGenericSyntax)
                        throw new XamlTransformException(
                            "x:Type generic arguments cannot be specified in both TypeName and x:TypeArguments",
                            textNode);

                    XamlAstXmlTypeReference ResolveSimpleTypeName(string typeName)
                    {
                        bool isNullable = typeName.EndsWith("?");
                        if (isNullable)
                            typeName = typeName.TrimEnd('?');

                        var pair = typeName.Split(new[] { ':' }, 2);
                        if (pair.Length == 1)
                            pair = new[] { "", pair[0] };

                        if (!context.NamespaceAliases.TryGetValue(pair[0].Trim(), out var resolvedNs))
                            throw new XamlTransformException($"Unable to resolve namespace {pair[0]}", textNode);

                        var typeRef = new XamlAstXmlTypeReference(textNode, resolvedNs, pair[1].Trim());

                        if (isNullable)
                            return MakeNullableTypeReference(typeRef);

                        return typeRef;
                    }

                    XamlAstXmlTypeReference ResolveTypeExpression(string typeExpression)
                    {
                        try
                        {
                            var parsed = CommaSeparatedParenthesesTreeParser.Parse(typeExpression);
                            if (parsed.Count != 1)
                                throw new XamlTransformException(
                                    "x:Type TypeName should contain exactly one type expression",
                                    textNode);

                            XamlAstXmlTypeReference ConvertNode(CommaSeparatedParenthesesTreeParser.Node parsedNode)
                            {
                                var parsedValue = parsedNode.Value;
                                if (parsedValue == null || string.IsNullOrWhiteSpace(parsedValue))
                                    throw new XamlTransformException("x:Type TypeName contains an empty type name", textNode);

                                var trimmed = parsedValue.Trim();
                                bool isNullable = trimmed.EndsWith("?");
                                if (isNullable)
                                    trimmed = trimmed.Substring(0, trimmed.Length - 1);

                                var typeReference = ResolveSimpleTypeName(trimmed);
                                if (parsedNode.Children.Count != 0)
                                    typeReference.GenericArguments = parsedNode.Children.Select(ConvertNode).ToList();

                                if (isNullable)
                                    return MakeNullableTypeReference(typeReference);

                                return typeReference;
                            }

                            return ConvertNode(parsed[0]);
                        }
                        catch (CommaSeparatedParenthesesTreeParser.ParseException e)
                        {
                            throw new XamlTransformException($"Unable to parse x:Type TypeName: {e.Message}", textNode, e);
                        }
                    }

                    var typeReference = hasInlineGenericSyntax
                        ? ResolveTypeExpression(typeRefText)
                        : ResolveSimpleTypeName(typeRefText);

                    if (!hasInlineGenericSyntax && xml.GenericArguments.Count > 0)
                        typeReference.GenericArguments = xml.GenericArguments;

                    return new XamlTypeExtensionNode(node,
                        typeReference,
                        context.Configuration.TypeSystem.GetType("System.Type"));

                    XamlAstXmlTypeReference MakeNullableTypeReference(XamlAstXmlTypeReference typeRef)
                    {
                        var resolved = TypeReferenceResolver.ResolveType(context, typeRef);
                        if (resolved.Type.IsNullable())
                            throw new XamlTransformException(
                                $"Type {resolved.Type.GetFqn()} is already nullable and cannot be made nullable again",
                                textNode);
                        if (resolved.Type.IsValueType)
                            return new XamlAstXmlTypeReference(textNode, XamlNamespaces.Xaml2006, "Nullable`1", [typeRef]);
                        return typeRef;
                    }
                }

                if (xml.Name == "Static")
                {
                    var textNode = ResolveArgumentOrValue("x:Static", "Member");
                    var typeRefText = textNode.Text.Trim();
                    var hasInlineGenericSyntax = typeRefText.IndexOfAny(new[] { '(', ')' }) != -1;

                    if (xml.GenericArguments.Count > 0 && hasInlineGenericSyntax)
                        throw new XamlTransformException(
                            "x:Static generic arguments cannot be specified in both Member and x:TypeArguments",
                            textNode);

                    var tmpair = typeRefText.Split(new[] { '.' }, 2);
                    if (tmpair.Length != 2)
                        throw new XamlTransformException($"Unable to parse {typeRefText} as 'type.member'", textNode);

                    XamlAstXmlTypeReference ResolveSimpleTypeName(string typeName)
                    {
                        bool isNullable = typeName.EndsWith("?");
                        if (isNullable) typeName = typeName.Substring(0, typeName.Length - 1);

                        var pair = typeName.Split(new[] { ':' }, 2);
                        if (pair.Length == 1)
                            pair = new[] { "", pair[0] };

                        if (!context.NamespaceAliases.TryGetValue(pair[0].Trim(), out var resolvedNs))
                            throw new XamlTransformException($"Unable to resolve namespace {pair[0]}", textNode);

                        var typeRef = new XamlAstXmlTypeReference(textNode, resolvedNs, pair[1].Trim());

                        if (isNullable)
                        {
                            var resolved = TypeReferenceResolver.ResolveType(context, typeRef);
                            if (resolved.Type.IsNullable())
                                throw new XamlTransformException(
                                    $"Type {resolved.Type.GetFqn()} is already nullable and cannot be made nullable again",
                                    textNode);
                            if (resolved.Type.IsValueType)
                                return new XamlAstXmlTypeReference(textNode, XamlNamespaces.Xaml2006, "Nullable`1",
                                    new[] { typeRef });
                        }

                        return typeRef;
                    }

                    XamlAstXmlTypeReference ResolveTypeExpression(string typeExpression)
                    {
                        try
                        {
                            var tree = CommaSeparatedParenthesesTreeParser.Parse(typeExpression);
                            if (tree.Count != 1)
                                throw new XamlTransformException(
                                    "x:Static Member should contain exactly one type expression",
                                    textNode);

                            XamlAstXmlTypeReference ConvertNode(CommaSeparatedParenthesesTreeParser.Node parsedNode)
                            {
                                var parsedValue = parsedNode.Value;
                                if (parsedValue == null || string.IsNullOrWhiteSpace(parsedValue))
                                    throw new XamlTransformException("x:Static Member contains an empty type name", textNode);

                                var trimmed = parsedValue.Trim();
                                bool isNullable = trimmed.EndsWith("?");
                                if (isNullable) trimmed = trimmed.Substring(0, trimmed.Length - 1);

                                var typeReference = ResolveSimpleTypeName(trimmed);
                                if (parsedNode.Children.Count != 0)
                                    typeReference.GenericArguments = parsedNode.Children.Select(ConvertNode).ToList();

                                if (isNullable)
                                {
                                    var resolved = TypeReferenceResolver.ResolveType(context, typeReference);
                                    if (resolved.Type.IsNullable())
                                        throw new XamlTransformException(
                                            $"Type {resolved.Type.GetFqn()} is already nullable and cannot be made nullable again",
                                            textNode);
                                    if (resolved.Type.IsValueType)
                                        return new XamlAstXmlTypeReference(textNode, XamlNamespaces.Xaml2006, "Nullable`1",
                                            new[] { typeReference });
                                }

                                return typeReference;
                            }

                            return ConvertNode(tree[0]);
                        }
                        catch (CommaSeparatedParenthesesTreeParser.ParseException e)
                        {
                            throw new XamlTransformException($"Unable to parse x:Static Member: {e.Message}", textNode, e);
                        }
                    }

                    var typeReference = hasInlineGenericSyntax
                        ? ResolveTypeExpression(tmpair[0])
                        : ResolveSimpleTypeName(tmpair[0]);

                    if (!hasInlineGenericSyntax && xml.GenericArguments.Count > 0)
                        typeReference.GenericArguments = xml.GenericArguments;

                    return new XamlStaticExtensionNode(ni,
                        typeReference,
                        tmpair[1]);
                }
            }

            return node;
        }
    }
}