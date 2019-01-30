using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace XamlX.Parsers
{
    public class XDocumentXamlXParserSettings
    {
        public Dictionary<string, string> CompatibleNamespaces { get; set; }
    }

    public class XDocumentXamlXParser
    {

        public static XamlXAstRootInstanceNode Parse(string s) => Parse(new StringReader(s));

        public static XamlXAstRootInstanceNode Parse(TextReader reader)
        {
            var root = XDocument.Load(reader, LoadOptions.SetLineInfo).Root;
            return new ParserContext(root).Parse();
        }


        class ParserContext
        {
            private readonly XElement _root;

            public ParserContext(XElement root)
            {
                _root = root;
            }


            IXamlXAstTypeReference GetTypeReference(XElement el) =>
                new XamlXAstXmlTypeReference(el.Name.NamespaceName, el.Name.LocalName).WithInfo(el);



            XamlXAstNewInstanceNode ParseNewInstance(XElement el, bool root)
            {
                if (el.Name.LocalName.Contains("."))
                    throw ParseError(el, "Dots aren't allowed on the element name");
                var type = GetTypeReference(el);
                var i = (root ? new XamlXAstRootInstanceNode(type) : new XamlXAstNewInstanceNode(type)).WithInfo(el);
                foreach (var attr in el.Attributes())
                {
                    if (attr.Name.NamespaceName == "http://www.w3.org/2000/xmlns/" ||
                        (attr.Name.NamespaceName == "" && attr.Name.LocalName == "xmlns"))
                    {
                        if (!root)
                            ParseError(attr,
                                "xmlns declarations are only allowed on the root element to preserve memory");
                        else
                        {
                            var name = attr.Name.NamespaceName == "" ? "" : attr.Name.LocalName;
                            ((XamlXAstRootInstanceNode) i).XmlNamespaces[name] = attr.Value;
                        }
                    }
                    else if (attr.Name.NamespaceName.StartsWith("http://www.w3.org"))
                    {
                        // Silently ignore all xml-parser related attributes
                    }
                    else if (attr.Name.NamespaceName != "" && !attr.Name.LocalName.Contains("."))
                        i.Children.Add(new XamlXAstXmlDirective(
                            attr.Name.NamespaceName, attr.Name.LocalName,
                            new XamlXAstTextNode(attr.Value).WithInfo(attr)
                        ).WithInfo(attr));
                    else
                    {
                        var pname = attr.Name.LocalName;
                        var ptype = i.Type;

                        if (pname.Contains("."))
                        {
                            var parts = pname.Split(new[] {'.'}, 2);
                            pname = parts[1];
                            var ns = attr.Name.Namespace == "" ? el.Name.NamespaceName : attr.Name.NamespaceName;
                            ptype = new XamlXAstXmlTypeReference(ns, parts[0])
                                .WithInfo(attr);
                        }

                        i.Children.Add(new XamlXAstPropertyAssignmentNode(
                            new XamlXAstNamePropertyReference(ptype, pname).WithInfo(attr),
                            new XamlXAstTextNode(attr.Value).WithInfo(attr)).WithInfo(attr));
                    }
                }


                foreach (var node in el.Nodes())
                {
                    if (node is XElement elementNode && elementNode.Name.LocalName.Contains("."))
                    {
                        if (elementNode.HasAttributes)
                            throw ParseError(node, "Attributes aren't allowed on element properties");
                        var pair = elementNode.Name.LocalName.Split(new[] {'.'}, 2);
                        i.Children.Add(new XamlXAstPropertyAssignmentNode(new XamlXAstNamePropertyReference
                            (

                                new XamlXAstXmlTypeReference(elementNode.Name.NamespaceName,
                                    pair[0]).WithInfo(node), pair[1]
                            ).WithInfo(node),
                            ParseValueNodeChildren(elementNode)
                        ).WithInfo(node));
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

            IXamlXAstValueNode ParseValueNode(XNode node)
            {
                if (node is XElement el)
                    return ParseNewInstance(el, false);
                if (node is XText text)
                    return new XamlXAstTextNode(text.Value).WithInfo(node);
                return null;
            }

            XamlXAstValueNodeList ParseValueNodeChildren(XElement parent)
            {
                var lst = new XamlXAstValueNodeList().WithInfo(parent);
                foreach (var n in parent.Nodes())
                {
                    var parsed = ParseValueNode(n);
                    if (parsed != null)
                        lst.Children.Add(parsed);
                    ;
                }

                return lst;
            }

            Exception ParseError(IXmlLineInfo line, string message) =>
                new XamlParseException(message, line.LineNumber, line.LinePosition);

            public XamlXAstRootInstanceNode Parse() => (XamlXAstRootInstanceNode) ParseNewInstance(_root, true);
        }
    }

    static class Extensions
    {
        public static T WithInfo<T>(this T node, IXmlLineInfo info) where T : IXamlXAstNode
        {
            node.Line = info.LineNumber;
            node.Position = info.LinePosition;

            return node;
        }
    }
}