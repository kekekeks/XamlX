using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace XamlIl.Parsers
{
    public class XDocumentXamlIlParserSettings
    {
        public Dictionary<string, string> CompatibleNamespaces { get; set; }
    }

    public class XDocumentXamlIlParser
    {

        public static XamlIlAstRootInstanceNode Parse(string s) => Parse(new StringReader(s));

        public static XamlIlAstRootInstanceNode Parse(TextReader reader)
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


            IXamlIlAstTypeReference GetTypeReference(XElement el) =>
                new XamlIlAstXmlTypeReference(el.Name.NamespaceName, el.Name.LocalName).WithInfo(el);



            XamlIlAstNewInstanceNode ParseNewInstance(XElement el, bool root)
            {
                if (el.Name.LocalName.Contains("."))
                    throw ParseError(el, "Dots aren't allowed on the element name");
                var type = GetTypeReference(el);
                var i = (root ? new XamlIlAstRootInstanceNode(type) : new XamlIlAstNewInstanceNode(type)).WithInfo(el);
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
                            ((XamlIlAstRootInstanceNode) i).XmlNamespaces[name] = attr.Value;
                        }
                    }
                    else if (attr.Name.NamespaceName.StartsWith("http://www.w3.org"))
                    {
                        // Silently ignore all xml-parser related attributes
                    }
                    else if (attr.Name.NamespaceName != "" && !attr.Name.LocalName.Contains("."))
                        i.Children.Add(new XamlIlAstXmlDirective(
                            attr.Name.NamespaceName, attr.Name.LocalName,
                            new XamlIlAstTextNode(attr.Value).WithInfo(attr)
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
                            ptype = new XamlIlAstXmlTypeReference(ns, parts[0])
                                .WithInfo(attr);
                        }

                        i.Children.Add(new XamlIlAstPropertyAssignmentNode(
                            new XamlIlAstNamePropertyReference(ptype, pname).WithInfo(attr),
                            new XamlIlAstTextNode(attr.Value).WithInfo(attr)).WithInfo(attr));
                    }
                }


                foreach (var node in el.Nodes())
                {
                    if (node is XElement elementNode && elementNode.Name.LocalName.Contains("."))
                    {
                        if (elementNode.HasAttributes)
                            throw ParseError(node, "Attributes aren't allowed on element properties");
                        var pair = elementNode.Name.LocalName.Split(new[] {'.'}, 2);
                        i.Children.Add(new XamlIlAstPropertyAssignmentNode(new XamlIlAstNamePropertyReference
                            (

                                new XamlIlAstXmlTypeReference(elementNode.Name.NamespaceName,
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

            IXamlIlAstValueNode ParseValueNode(XNode node)
            {
                if (node is XElement el)
                    return ParseNewInstance(el, false);
                if (node is XText text)
                    return new XamlIlAstTextNode(text.Value).WithInfo(node);
                return null;
            }

            XamlIlAstValueNodeList ParseValueNodeChildren(XElement parent)
            {
                var lst = new XamlIlAstValueNodeList().WithInfo(parent);
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

            public XamlIlAstRootInstanceNode Parse() => (XamlIlAstRootInstanceNode) ParseNewInstance(_root, true);
        }
    }

    static class Extensions
    {
        public static T WithInfo<T>(this T node, IXmlLineInfo info) where T : IXamlIlAstNode
        {
            node.Line = info.LineNumber;
            node.Position = info.LinePosition;

            return node;
        }
    }
}