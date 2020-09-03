using Microsoft.Language.Xml;
using PimpMyAvalonia.LanguageServer;
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
    class GuiLabsXamlParser
    {

        public static XamlDocument Parse(string s, Dictionary<string, string> compatibilityMappings = null)
        {
            return Parse(new StringReader(s), compatibilityMappings);
        }

        public static XamlDocument Parse(TextReader reader, Dictionary<string, string> compatibilityMappings = null)
        {
            string data = reader.ReadToEnd();
            var buffer = new StringBuffer(data);
            var parsed = Parser.Parse(buffer);

            Dictionary<string, string> namespaceAliases = new Dictionary<string, string>();
            HashSet<string> ignorableNamespaces = new HashSet<string>();
            const string ignorableNs = "http://schemas.openxmlformats.org/markup-compatibility/2006";
            ignorableNamespaces.Add(ignorableNs);
            var attributes = parsed.Root.Attributes
                .Select(n =>
                {
                    var pn = XmlNamespaces.GetPrefixFromName(n.Key);
                    return (pn.prefix, pn.name, n.Value);
                    
                })
                .ToList();
            foreach (var attr in attributes)
            {
                if (attr.prefix == "xmlns")
                {
                    namespaceAliases[attr.name] = attr.Value;
                }

                if (attr.prefix == "" && attr.name == "xmlns")
                {
                    namespaceAliases[""] = attr.Value;
                }
            }

            foreach (var attr in attributes)
            {
                if (attr.name == "Ignorable" && namespaceAliases.TryGetValue(attr.prefix, out var transformedNs) && transformedNs == ignorableNs)
                {
                    foreach (var ignorable in attr.Value.Split(' '))
                    {
                        ignorableNamespaces.Add(namespaceAliases[ignorable]);
                    }
                }
            }

            if(compatibilityMappings == null)
            {
                compatibilityMappings = new Dictionary<string, string>();
            }


            XmlNamespaces ns = new XmlNamespaces(namespaceAliases, ignorableNamespaces, compatibilityMappings);
            var doc = new XamlDocument
            {
                Root = new ParserContext(parsed.Root, data, ns).Parse(),
                NamespaceAliases = namespaceAliases
            };

            return doc;
        }


        class ParserContext
        {
            private readonly IXmlElement _newRoot;
            private readonly string _text;
            private readonly XmlNamespaces _ns;

            public ParserContext(IXmlElement newRoot, string text, XmlNamespaces ns)
            {
                _newRoot = newRoot;
                _text = text;
                _ns = ns;
            }

            XamlAstXmlTypeReference GetTypeReference(IXmlElement el)
            {
                (string ns, _, string name) = _ns.GetNsFromName(el.Name);
                return new XamlAstXmlTypeReference(el.AsLi(_text), ns, name);
            }

            XamlAstXmlTypeReference ParseTypeName(IXamlLineInfo info, string typeName)
                => ParseTypeName(info, typeName,
                    ns => string.IsNullOrWhiteSpace(ns)
                        ? _ns.DefaultNamespace
                        : _ns.NsForPrefix(ns) ?? "");

            static XamlAstXmlTypeReference ParseTypeName(IXamlLineInfo info, string typeName, Func<string, string> prefixResolver)
            {
                var pair = typeName.Trim().Split(new[] { ':' }, 2);
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
                        throw new XamlParseException($"Namespace '{pair[0]}' is not recognized", info);
                    name = pair[1];
                }
                return new XamlAstXmlTypeReference(info, xmlns, name);
            }

            List<XamlAstXmlTypeReference> ParseTypeArguments(string args, IXmlElement xel, IXamlLineInfo info)
            {
                try
                {
                    XamlAstXmlTypeReference Parse(CommaSeparatedParenthesesTreeParser.Node node)
                    {
                        var rv = ParseTypeName(info, node.Value);

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

            IXamlAstValueNode ParseTextValueOrMarkupExtension(string ext, IXamlLineInfo info)
            {
                if (ext.StartsWith("{") || ext.StartsWith(@"\{"))
                {
                    if (ext.StartsWith("{}"))
                        ext = ext.Substring(2);
                    else
                    {
                        try
                        {
                            return SystemXamlMarkupExtensionParser.SystemXamlMarkupExtensionParser.Parse(info, ext,
                                t => ParseTypeName(info, t));
                        }
                        catch (MeScannerParseException parseEx)
                        {
                            throw new XamlParseException(parseEx.Message, info);
                        }
                    }
                }

                ext = UnescapeXml(ext, false);
                return new XamlAstTextNode(info, ext);
            }

            XamlAstObjectNode ParseNewInstance(IXmlElement newEl, bool root)
            {
                XamlAstXmlTypeReference type;
                XamlAstObjectNode i;

                (string _, string elementName) = XmlNamespaces.GetPrefixFromName(newEl.Name);

                if (elementName.Contains("."))
                    throw ParseError(newEl.AsLi(_text), "Dots aren't allowed in type names");
                type = GetTypeReference(newEl);
                i = new XamlAstObjectNode(newEl.AsLi(_text), type);

                foreach (XmlAttributeSyntax attribute in newEl.AsSyntaxElement.Attributes)
                {
                    (string attrNs, string attrPrefix, string attrName) = _ns.GetNsFromName(attribute.Name);
                    if (_ns.IsIgnorable(attrNs))
                    {
                        continue;
                    }
                    if (attrNs == "http://www.w3.org/2000/xmlns/" || attrPrefix == "xmlns" ||
                        (attrPrefix == "" && attrName == "xmlns"))
                    {

                        if (!root)
                            throw ParseError(attribute.AsLi(_text),
                                "xmlns declarations are only allowed on the root element to preserve memory");
                    }
                    else if (attrNs.StartsWith("http://www.w3.org"))
                    {
                        // Silently ignore all xml-parser related attributes
                    }
                    // Parse type arguments
                    else if (attrNs == XamlNamespaces.Xaml2006 &&
                                attrName == "TypeArguments")
                        type.GenericArguments = ParseTypeArguments(attribute.Value, newEl, attribute.AsLi(_text));
                    // Parse as a directive
                    else if (attrPrefix != "" && !attrName.Contains("."))
                        i.Children.Add(new XamlAstXmlDirective(newEl.AsLi(_text),
                            attrNs, attrName, new[]
                            {
                            ParseTextValueOrMarkupExtension(attribute.Value, attribute.AsLi(_text))
                            }
                        ));
                    // Parse as a property
                    else
                    {
                        var pname = attrName;
                        var ptype = i.Type;

                        if (pname.Contains("."))
                        {
                            var parts = pname.Split(new[] { '.' }, 2);
                            pname = parts[1];
                            var ns = attrPrefix == "" ? _ns.DefaultNamespace : attrNs;
                            ptype = new XamlAstXmlTypeReference(newEl.AsLi(_text), ns, parts[0]);
                        }

                        i.Children.Add(new XamlAstXamlPropertyValueNode(newEl.AsLi(_text),
                            new XamlAstNamePropertyReference(newEl.AsLi(_text), ptype, pname, type),
                            ParseTextValueOrMarkupExtension(attribute.Value, attribute.AsLi(_text))));
                    }
                }

                foreach (var newNode in newEl.Elements)
                {
                    (string nodeNs, string nodePrefix, string nodeName) = _ns.GetNsFromName(newNode.Name);
                    if (_ns.IsIgnorable(nodeNs))
                    {
                        continue;
                    }

                   if (nodeName.Contains("."))
                    {
                        if (newNode.Attributes.Any())
                            throw ParseError(newNode.AsLi(_text), "Attributes aren't allowed on element properties");
                        var pair = nodeName.Split(new[] { '.' }, 2);
                        i.Children.Add(new XamlAstXamlPropertyValueNode(newEl.AsLi(_text), new XamlAstNamePropertyReference
                            (
                                newEl.AsLi(_text),
                                new XamlAstXmlTypeReference(newEl.AsLi(_text), nodeNs,
                                    pair[0]), pair[1], type
                            ),
                            ParseValueNodeChildren(newNode)
                        ));
                    }
                    else
                    {
                        i.Children.Add(ParseNewInstance(newNode, false));
                    }

                }

                SyntaxList<SyntaxNode> syntaxContent = newEl.AsSyntaxElement.Content;
                if (TryParseText(newEl, out var textNode))
                {
                    i.Children.Add(textNode);
                }

                return i;
            }

            List<IXamlAstValueNode> ParseValueNodeChildren(IXmlElement newParent)
            {
                var lst = new List<IXamlAstValueNode>();

                if (TryParseText(newParent, out var node))
                {
                    lst.Add(node);
                }
                else
                {
                    foreach (var newNode in newParent.Elements)
                    {
                        lst.Add(ParseNewInstance(newNode, false));
                    }
                }
                return lst;
            }

            public bool TryParseText(IXmlElement element, out IXamlAstValueNode node)
            {
                if (element != null && element.AsSyntaxElement.Content.Count == 1 && element.AsSyntaxElement.Content[0] is XmlTextSyntax textContent)
                {
                    //using (var reader = XmlReader.Create(new StringReader("<a>" +textContent.Value + "</a>")))
                    //{
                    //    reader.MoveToContent();
                    //    string unescapedText = reader.ReadElementContentAsString();
                    //}
                    //XmlDocument doc = new XmlDocument();
                    //string xml = "<a>" + textContent.Value + "</a>";
                    //doc.LoadXml(xml);
                    //unescapedText = doc.DocumentElement.InnerText;

                    string text = UnescapeXml(textContent.Value);
                    node = new XamlAstTextNode(textContent.AsLi(_text), text.Trim());
                    return true;
                }
                node = null;
                return false;
            }

            private string UnescapeXml(string input, bool preserveNewlines = true)
            {
                if (!preserveNewlines)
                {
                    input = input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
                }
                
                return XElement.Parse("<a>" + input + "</a>").Value;

            }

            Exception ParseError(IXamlLineInfo line, string message) =>
                new XamlParseException(message, line.Line, line.Position);

            public XamlAstObjectNode Parse() => (XamlAstObjectNode)ParseNewInstance(_newRoot, true);
        }
    }

    static class GuilabsExtensions
    {
        public static IXamlLineInfo AsLi(this SyntaxNode info, string data)
        {
            return TextPosition.OffsetToPosition((info).SpanStart + 1, data);
        }
        public static IXamlLineInfo AsLi(this IXmlElement info, string data)
        {
            return TextPosition.OffsetToPosition(((XmlNodeSyntax)info).SpanStart + 1, data);
        }
    }
}
