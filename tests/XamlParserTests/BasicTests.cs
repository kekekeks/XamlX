using System;
using System.Collections.Generic;
using System.Xml.Linq;
using XamlX;
using XamlX.Parsers;
using Xunit;

namespace XamlParserTests
{
    public class BasicTests
    {
        [Fact]
        public void Test1()
        {
            var root = XDocumentXamlParser.Parse(
                @"
<Root xmlns='rootns' xmlns:t='testns' xmlns:x='directive'>
    <Child 
        Other.Prop='{TestExt something}'
        Prop1='123' 
        Root.AttachedProp='AttachedValue'
        t:Namespaced.AttachedProp='AttachedValue'
        x:Directive='DirectiveValue'>
        <t:SubChild Prop='321'/>
        <Child.DottedProp>DottedValue</Child.DottedProp>
        <Root.AttachedDottedProp>AttachedValue</Root.AttachedDottedProp>
        <Child.NodeListProp>
            <SubChild/>
            <SubChild/>
        </Child.NodeListProp>
    </Child>

</Root>");
            var other = new XamlXAstRootInstanceNode(new XamlAstXmlTypeReference("rootns", "Root"))
            {
                XmlNamespaces = new Dictionary<string, string>
                {
                    [""] = "rootns",
                    ["t"] = "testns",
                    ["x"] = "directive"
                },
                Children =
                {
                    // <Child
                    new XamlXAstNewInstanceNode(new XamlAstXmlTypeReference("rootns", "Child"))
                    {
                        Children =
                        {
                            // Other.Prop='{TestExt something}'
                            new XamlXAstPropertyAssignmentNode(new XamlAstNamePropertyReference(
                                    new XamlAstXmlTypeReference("rootns", "Other"), "Prop"),
                                new XamlAstTextNode("{TestExt something}")),
                            //  Prop1='123' 
                            new XamlXAstPropertyAssignmentNode(new XamlAstNamePropertyReference(
                                    new XamlAstXmlTypeReference("rootns", "Child"), "Prop1"),
                                new XamlAstTextNode("123")),
                            // Root.AttachedProp='AttachedValue'
                            new XamlXAstPropertyAssignmentNode(new XamlAstNamePropertyReference(
                                    new XamlAstXmlTypeReference("rootns", "Root"), "AttachedProp"),
                                new XamlAstTextNode("AttachedValue")),
                            //t:Namespaced.AttachedProp='AttachedValue'
                            new XamlXAstPropertyAssignmentNode(new XamlAstNamePropertyReference(
                                    new XamlAstXmlTypeReference("testns", "Namespaced"), "AttachedProp"),
                                new XamlAstTextNode("AttachedValue")),
                            //x:Directive='DirectiveValue'>
                            new XamlAstXmlDirective("directive", "Directive", new XamlAstTextNode("DirectiveValue")),
                            // <t:SubChild Prop='321'/>
                            new XamlXAstNewInstanceNode(new XamlAstXmlTypeReference("testns", "SubChild"))
                            {
                                Children =
                                {
                                    new XamlXAstPropertyAssignmentNode(new XamlAstNamePropertyReference(
                                            new XamlAstXmlTypeReference("testns", "SubChild"), "Prop"),
                                        new XamlAstTextNode("321"))
                                }
                            },
                            //<Child.DottedProp>DottedValue</Child.DottedProp>
                            new XamlXAstPropertyAssignmentNode(new XamlAstNamePropertyReference(
                                    new XamlAstXmlTypeReference("rootns", "Child"), "DottedProp"),
                                new XamlXAstValueNodeList
                                {
                                    Children = {new XamlAstTextNode("DottedValue")}
                                }),
                            // <Root.AttachedDottedProp>AttachedValue</Root.AttachedDottedProp>
                            new XamlXAstPropertyAssignmentNode(new XamlAstNamePropertyReference(
                                    new XamlAstXmlTypeReference("rootns", "Root"), "AttachedDottedProp"),
                                new XamlXAstValueNodeList
                                {
                                    Children = {new XamlAstTextNode("AttachedValue")}
                                }),
                            //<Child.NodeListProp>
                            new XamlXAstPropertyAssignmentNode(new XamlAstNamePropertyReference(
                                    new XamlAstXmlTypeReference("rootns", "Child"), "NodeListProp"),
                                new XamlXAstValueNodeList
                                {
                                    Children =
                                    {
                                        // <SubChild/>
                                        new XamlXAstNewInstanceNode(new XamlAstXmlTypeReference("rootns", "SubChild")),
                                        // <SubChild/>
                                        new XamlXAstNewInstanceNode(new XamlAstXmlTypeReference("rootns", "SubChild")),
                                    }
                                })
                        }
                    },

                }
            };
            Helpers.StructDiff(root, other);

        }
    }
}