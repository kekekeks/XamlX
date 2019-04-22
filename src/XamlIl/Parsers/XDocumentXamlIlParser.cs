using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using XamlIl.Ast;

namespace XamlIl.Parsers
{
    public class XDocumentXamlIlParserSettings
    {
        public Dictionary<string, string> CompatibleNamespaces { get; set; }
    }

    public class XDocumentXamlIlParser
    {

        public static XamlIlDocument Parse(string s, Dictionary<string, string> compatibilityMappings = null)
        {
            return Parse(new StringReader(s), compatibilityMappings);
        }

        public static XamlIlDocument Parse(TextReader reader, Dictionary<string, string> compatibilityMappings = null)
        {
            var xr = XmlReader.Create(reader, new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                DtdProcessing = DtdProcessing.Ignore
            });
            xr = new CompatibleXmlReader(xr, compatibilityMappings ?? new Dictionary<string, string>());
            
            var root = XDocument.Load(xr, LoadOptions.SetLineInfo).Root;

            var doc = new XamlIlDocument
            {
                Root = new ParserContext(root).Parse()
            };
            
            foreach(var attr in root.Attributes())
                if (attr.Name.NamespaceName == "http://www.w3.org/2000/xmlns/" ||
                    (attr.Name.NamespaceName == "" && attr.Name.LocalName == "xmlns"))
                {
                    var name = attr.Name.NamespaceName == "" ? "" : attr.Name.LocalName;
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


            XamlIlAstXmlTypeReference GetTypeReference(XElement el) =>
                new XamlIlAstXmlTypeReference(el.AsLi(), el.Name.NamespaceName, el.Name.LocalName);


            static XamlIlAstXmlTypeReference ParseTypeName(IXamlIlLineInfo info, string typeName, XElement xel)
                => ParseTypeName(info, typeName,
                    ns => string.IsNullOrWhiteSpace(ns)
                        ? xel.GetDefaultNamespace().NamespaceName
                        : xel.GetNamespaceOfPrefix(ns)?.NamespaceName ?? "");
            
            static XamlIlAstXmlTypeReference ParseTypeName(IXamlIlLineInfo info, string typeName, Func<string, string> prefixResolver)
            {
                var pair = typeName.Trim().Split(new[] {':'}, 2);
                string xmlns, name;
                if (pair.Length == 1)
                {
                    xmlns = prefixResolver("");
                    name = pair[0];
                }
                else
                {
                    xmlns = prefixResolver(pair[0]);
                    if (xmlns == null)
                        throw new XamlIlParseException($"Namespace '{pair[0]}' is not recognized", info);
                    name = pair[1];
                }
                return new XamlIlAstXmlTypeReference(info, xmlns, name);
            }

            static List<XamlIlAstXmlTypeReference> ParseTypeArguments(string args, XElement xel, IXamlIlLineInfo info)
            {
                try
                {
                    XamlIlAstXmlTypeReference Parse(CommaSeparatedParenthesesTreeParser.Node node)
                    {
                        var rv = ParseTypeName(info, node.Value, xel);

                        if (node.Children.Count != 0)
                            rv.GenericArguments = node.Children.Select(Parse).ToList();
                        return rv;
                    }
                    var tree = CommaSeparatedParenthesesTreeParser.Parse(args);
                    return tree.Select(Parse).ToList();
                }
                catch (CommaSeparatedParenthesesTreeParser.ParseException e)
                {
                    throw new XamlIlParseException(e.Message, info);
                }
            }

            static IXamlIlAstValueNode ParseTextValueOrMarkupExtension(string ext, XElement xel, IXamlIlLineInfo info)
            {
                if (ext.StartsWith("{"))
                {
                    if (ext.StartsWith("{}"))
                        ext = ext.Substring(2);
                    else
                        return XamlMarkupExtensionParser.Parse(info, ext, t => ParseTypeName(info, t, xel));
                }

                return new XamlIlAstTextNode(info, ext);
            }

            XamlIlAstObjectNode ParseNewInstance(XElement el, bool root)
            {
                if (el.Name.LocalName.Contains("."))
                    throw ParseError(el, "Dots aren't allowed in type names");
                var type = GetTypeReference(el);
                var i = new XamlIlAstObjectNode(el.AsLi(), type);
                foreach (var attr in el.Attributes())
                {
                    if (attr.Name.NamespaceName == "http://www.w3.org/2000/xmlns/" ||
                        (attr.Name.NamespaceName == "" && attr.Name.LocalName == "xmlns"))
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
                    else if (attr.Name.NamespaceName != "" && !attr.Name.LocalName.Contains("."))
                        i.Children.Add(new XamlIlAstXmlDirective(el.AsLi(),
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

                        if (pname.Contains("."))
                        {
                            var parts = pname.Split(new[] {'.'}, 2);
                            pname = parts[1];
                            var ns = attr.Name.Namespace == "" ? el.GetDefaultNamespace().NamespaceName : attr.Name.NamespaceName;
                            ptype = new XamlIlAstXmlTypeReference(el.AsLi(), ns, parts[0]);
                        }

                        i.Children.Add(new XamlIlAstXamlPropertyValueNode(el.AsLi(),
                            new XamlIlAstNamePropertyReference(el.AsLi(), ptype, pname, type),
                            ParseTextValueOrMarkupExtension(attr.Value, el, attr.AsLi())));
                    }
                }


                foreach (var node in el.Nodes())
                {
                    if (node is XElement elementNode && elementNode.Name.LocalName.Contains("."))
                    {
                        if (elementNode.HasAttributes)
                            throw ParseError(node, "Attributes aren't allowed on element properties");
                        var pair = elementNode.Name.LocalName.Split(new[] {'.'}, 2);
                        i.Children.Add(new XamlIlAstXamlPropertyValueNode(el.AsLi(), new XamlIlAstNamePropertyReference
                            (
                                el.AsLi(),
                                new XamlIlAstXmlTypeReference(el.AsLi(), elementNode.Name.NamespaceName,
                                    pair[0]), pair[1], type
                            ),
                            ParseValueNodeChildren(elementNode)
                        ));
                    }
                    else
                    {
                        var parsed = ParseValueNode(node);
                        if (parsed != null)
                            i.Children.Add(parsed);
                    }

                }

                return i;
            }

            IXamlIlAstValueNode ParseValueNode(XNode node)
            {
                if (node is XElement el)
                    return ParseNewInstance(el, false);
                if (node is XText text)
                    return new XamlIlAstTextNode(node.AsLi(), text.Value);
                return null;
            }

            List<IXamlIlAstValueNode> ParseValueNodeChildren(XElement parent)
            {
                var lst = new List<IXamlIlAstValueNode>();
                foreach (var n in parent.Nodes())
                {
                    var parsed = ParseValueNode(n);
                    if (parsed != null)
                        lst.Add(parsed);
                }
                return lst;
            }

            Exception ParseError(IXmlLineInfo line, string message) =>
                new XamlIlParseException(message, line.LineNumber, line.LinePosition);

            public XamlIlAstObjectNode Parse() => (XamlIlAstObjectNode) ParseNewInstance(_root, true);
        }
    }

    static class Extensions
    {
        class WrappedLineInfo : IXamlIlLineInfo
        {
            public WrappedLineInfo(IXmlLineInfo info)
            {
                Line = info.LineNumber;
                Position = info.LinePosition;
            }
            public int Line { get; set; }
            public int Position { get; set; }
        }
        
        public static IXamlIlLineInfo AsLi(this IXmlLineInfo info)
        {
            if (!info.HasLineInfo())
                throw new InvalidOperationException("XElement doesn't have line info");
            return new WrappedLineInfo(info);
        }

    }
}
