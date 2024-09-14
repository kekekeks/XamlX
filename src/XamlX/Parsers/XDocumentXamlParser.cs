using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using XamlX.Ast;
using XamlX.Parsers.SystemXamlMarkupExtensionParser;

namespace XamlX.Parsers
{
#if !XAMLX_INTERNAL
    public
#endif
    class XDocumentXamlParserSettings
    {
        public Dictionary<string, string>? CompatibleNamespaces { get; set; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XDocumentXamlParser
    {

        public static XamlDocument Parse(string s, Dictionary<string, string>? compatibilityMappings = null)
        {
            return Parse(new StringReader(s), compatibilityMappings);
        }

        public static XamlDocument Parse(TextReader reader, Dictionary<string, string>? compatibilityMappings = null)
        {
            var xr = XmlReader.Create(reader, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore
            });
            xr = new CompatibleXmlReader(xr, compatibilityMappings ?? new Dictionary<string, string>());

            var root = XDocument.Load(xr, LoadOptions.SetLineInfo).Root!;

            var doc = new XamlDocument
            {
                Root = new ParserContext(root).Parse()
            };

            foreach(var attr in root.Attributes())
                if (attr.Name.NamespaceName == "http://www.w3.org/2000/xmlns/" ||
                    (string.IsNullOrEmpty(attr.Name.NamespaceName) && attr.Name.LocalName == "xmlns"))
                {
                    var name = string.IsNullOrEmpty(attr.Name.NamespaceName) ? "" : attr.Name.LocalName;
                    doc.NamespaceAliases[name] = attr.Value;
                }

            return doc;
        }


        class ParserContext
        {
            private readonly XElement _root;

            public ParserContext(XElement root)
            {
                _root = root;
            }


            XamlAstXmlTypeReference GetTypeReference(XElement el) =>
                new XamlAstXmlTypeReference(el.AsLi(), el.Name.NamespaceName, el.Name.LocalName);

            static XamlAstXmlTypeReference ParseTypeName(IXamlLineInfo info, string typeName, XElement xel)
            {
                var (_, xmlnsVal, name) = ParsePairWithXmlns(info, typeName, xel);
                return new XamlAstXmlTypeReference(info, xmlnsVal, name);
            }

            static (string xmlnsKey, string xmlnsVal, string name) ParsePairWithXmlns(IXamlLineInfo info, string typeName, XElement xel)
            {
                var pair = typeName.Trim().Split(new[] {':'}, 2);
                string xmlnsKey, xmlnsVal, name;
                if (pair.Length == 1)
                {
                    xmlnsKey = "";
                    xmlnsVal = PrefixResolver("", xel);
                    name = pair[0];
                }
                else
                {
                    xmlnsKey = pair[0];
                    xmlnsVal = PrefixResolver(pair[0], xel);
                    if (xmlnsVal == null)
                        throw new XamlParseException($"Namespace '{pair[0]}' is not recognized", info);
                    name = pair[1];
                }

                return (xmlnsKey, xmlnsVal, name);
            }

            static string PrefixResolver(string ns, XElement xel)
                => string.IsNullOrWhiteSpace(ns)
                    ? xel.GetDefaultNamespace().NamespaceName
                    : xel.GetNamespaceOfPrefix(ns)?.NamespaceName ?? "";

            static List<XamlAstXmlTypeReference> ParseTypeArguments(string args, XElement xel, IXamlLineInfo info)
            {
                try
                {
                    XamlAstXmlTypeReference Parse(CommaSeparatedParenthesesTreeParser.Node node)
                    {
                        var rv = ParseTypeName(info, node.Value!, xel);

                        if (node.Children.Count != 0)
                            rv.GenericArguments = node.Children.Select(Parse).ToList();
                        return rv;
                    }
                    var tree = CommaSeparatedParenthesesTreeParser.Parse(args);
                    return tree.Select(Parse).ToList();
                }
                catch (CommaSeparatedParenthesesTreeParser.ParseException e)
                {
                    throw new XamlParseException(e.Message, info);
                }
            }

            static IXamlAstValueNode ParseTextValueOrMarkupExtension(string ext, XElement xel, IXamlLineInfo info)
            {
                if (ext.StartsWith("{") || ext.StartsWith(@"\{"))
                {
                    if (ext.StartsWith("{}"))
                        ext = ext.Substring(2);
                    else
                    {
                        try
                        {

                            var extensionObject = SystemXamlMarkupExtensionParser.SystemXamlMarkupExtensionParser.Parse(info, ext,
                                t => ParseTypeName(info, t, xel));

                            if (extensionObject is XamlAstObjectNode astObject)
                            {
                                TransformMarkupExtensionNodeProperties(astObject, xel);
                            }

                            return extensionObject;
                        }
                        catch (MeScannerParseException parseEx)
                        {
                            throw new XamlParseException(parseEx.Message, info);
                        }
                    }
                }

                // Do not apply XAML whitespace normalization to attribute values
                return new XamlAstTextNode(info, ext, true);
            }


            static void TransformMarkupExtensionNodeProperties(XamlAstObjectNode astObject, XElement xel)
            {
                var xmlType = (XamlAstXmlTypeReference)astObject.Type;

                foreach (var prop in astObject.Children.ToArray())
                {
                    if (prop is XamlAstXamlPropertyValueNode { Property: XamlAstNamePropertyReference propName } valueNode)
                    {
                        var (xmlnsKey, xmlnsVal, name) = ParsePairWithXmlns(prop, propName.Name, xel);
                        if ((xmlnsVal, name) is (XamlNamespaces.Xaml2006, "TypeArguments"))
                        {
                            if (valueNode.Values.Single() is not XamlAstTextNode text)
                                throw new XamlParseException(
                                    "Unable to resolve TypeArguments. String node with one or multiple type arguments is expected.",
                                    prop);

                            xmlType.GenericArguments.AddRange(ParseTypeArguments(text.Text, xel, prop));
                            astObject.Children.Remove(prop);
                        }
                        else if (!string.IsNullOrEmpty(xmlnsKey) && !name.Contains('.'))
                        {
                            astObject.Children.Add(new XamlAstXmlDirective(prop, xmlnsVal, name, valueNode.Values));
                            astObject.Children.Remove(prop);
                        }
                        else if (valueNode.Values.FirstOrDefault() is XamlAstObjectNode childAstObject)
                        {
                            TransformMarkupExtensionNodeProperties(childAstObject, xel);
                        }
                    }
                }
            }

            XamlAstObjectNode ParseNewInstance(XElement el, bool root, XmlSpace spaceMode)
            {
                var declaredMode = el.GetDeclaredWhitespaceMode();
                if (declaredMode != XmlSpace.None)
                {
                    spaceMode = declaredMode;
                }

                if (el.Name.LocalName.Contains('.'))
                    throw ParseError(el, "Dots aren't allowed in type names");
                var type = GetTypeReference(el);
                var i = new XamlAstObjectNode(el.AsLi(), type);
                foreach (var attr in el.Attributes())
                {
                    if (attr.Name.NamespaceName == "http://www.w3.org/2000/xmlns/" ||
                        (string.IsNullOrEmpty(attr.Name.NamespaceName) && attr.Name.LocalName == "xmlns"))
                    {
                        if (!root)
                            throw ParseError(attr,
                                "xmlns declarations are only allowed on the root element to preserve memory");
                    }
                    else if (attr.Name.NamespaceName.StartsWith("http://www.w3.org"))
                    {
                        // Silently ignore all xml-parser related attributes
                    }
                    // Parse type arguments
                    else if (attr.Name.NamespaceName == XamlNamespaces.Xaml2006 &&
                             attr.Name.LocalName == "TypeArguments")
                        type.GenericArguments = ParseTypeArguments(attr.Value, el, attr.AsLi());
                    // Parse as a directive
                    else if (!string.IsNullOrEmpty(attr.Name.NamespaceName) && !attr.Name.LocalName.Contains('.'))
                        i.Children.Add(new XamlAstXmlDirective(el.AsLi(),
                            attr.Name.NamespaceName, attr.Name.LocalName, new[]
                            {
                                ParseTextValueOrMarkupExtension(attr.Value, el, attr.AsLi())
                            }
                        ));
                    // Parse as a property
                    else
                    {
                        var pname = attr.Name.LocalName;
                        var ptype = i.Type;

                        if (pname.Contains('.'))
                        {
                            var parts = pname.Split(new[] {'.'}, 2);
                            pname = parts[1];
                            var ns = attr.Name.Namespace == "" ? el.GetDefaultNamespace().NamespaceName : attr.Name.NamespaceName;
                            ptype = new XamlAstXmlTypeReference(el.AsLi(), ns, parts[0]);
                        }

                        i.Children.Add(new XamlAstXamlPropertyValueNode(el.AsLi(),
                            new XamlAstNamePropertyReference(el.AsLi(), ptype, pname, type),
                            ParseTextValueOrMarkupExtension(attr.Value, el, attr.AsLi()), true));
                    }
                }


                foreach (var node in el.Nodes())
                {
                    if (node is XElement elementNode && elementNode.Name.LocalName.Contains('.'))
                    {
                        if (elementNode.HasAttributes)
                            throw ParseError(node, "Attributes aren't allowed on element properties");
                        var pair = elementNode.Name.LocalName.Split(new[] {'.'}, 2);
                        i.Children.Add(new XamlAstXamlPropertyValueNode(el.AsLi(), new XamlAstNamePropertyReference
                            (
                                el.AsLi(),
                                pair[0] == type.Name && elementNode.Name.NamespaceName == type.XmlNamespace ? type : 
                                new XamlAstXmlTypeReference(el.AsLi(), elementNode.Name.NamespaceName,
                                    pair[0]), pair[1], type
                            ),
                            ParseValueNodeChildren(elementNode, spaceMode),
                            false
                        ));
                    }
                    else
                    {
                        var parsed = ParseValueNode(node, spaceMode);
                        if (parsed != null)
                            i.Children.Add(parsed);
                    }

                }

                return i;
            }

            IXamlAstValueNode? ParseValueNode(XNode node, XmlSpace spaceMode)
            {
                if (node is XElement el)
                    return ParseNewInstance(el, false, spaceMode);
                if (node is XText text)
                {
                    var preserveWhitespace = spaceMode == XmlSpace.Preserve;
                    return new XamlAstTextNode(node.AsLi(), text.Value, preserveWhitespace);
                }

                return null;
            }

            List<IXamlAstValueNode> ParseValueNodeChildren(XElement parent, XmlSpace spaceMode)
            {
                var lst = new List<IXamlAstValueNode>();
                foreach (var n in parent.Nodes())
                {
                    var parsed = ParseValueNode(n, spaceMode);
                    if (parsed != null)
                        lst.Add(parsed);
                }
                return lst;
            }

            Exception ParseError(IXmlLineInfo line, string message) =>
                new XamlParseException(message, line.LineNumber, line.LinePosition);

            public XamlAstObjectNode Parse() => (XamlAstObjectNode) ParseNewInstance(_root, true, XmlSpace.Default);
        }
    }

    static class Extensions
    {
        class WrappedLineInfo : IXamlLineInfo
        {
            public WrappedLineInfo(IXmlLineInfo info)
            {
                Line = info.LineNumber;
                Position = info.LinePosition;
            }
            public int Line { get; set; }
            public int Position { get; set; }
        }

        public static IXamlLineInfo AsLi(this IXmlLineInfo info)
        {
            if (!info.HasLineInfo())
                throw new InvalidOperationException("XElement doesn't have line info");
            return new WrappedLineInfo(info);
        }

        private static readonly XName SpaceAttributeName = XName.Get("space", XNamespace.Xml.NamespaceName);

        // Get the xml:space mode declared on the node - if it's an element, None otherwise.
        public static XmlSpace GetDeclaredWhitespaceMode(this XNode node)
        {
            if (node is XElement element)
            {
                var declaredMode = element.Attribute(SpaceAttributeName);
                if (declaredMode == null)
                {
                    return XmlSpace.None;
                }

                switch (declaredMode.Value)
                {
                    case "default":
                        return XmlSpace.Default;
                    case "preserve":
                        return XmlSpace.Preserve;
                    default:
                        return XmlSpace.None;
                }
            }
            else
            {
                return XmlSpace.None;
            }
        }
    }
}
