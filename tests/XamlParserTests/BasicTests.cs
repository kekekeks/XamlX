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
            var root = XDocumentXamlXParser.Parse(
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
            var other = new XamlXAstRootInstanceNode(new XamlXAstXmlTypeReference("rootns", "Root"))
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
                    new XamlXAstNewInstanceNode(new XamlXAstXmlTypeReference("rootns", "Child"))
                    {
                        Children =
                        {
                            // Other.Prop='{TestExt something}'
                            new XamlXAstPropertyAssignmentNode(new XamlXAstNamePropertyReference(
                                    new XamlXAstXmlTypeReference("rootns", "Other"), "Prop"),
                                new XamlXAstTextNode("{TestExt something}")),
                            //  Prop1='123' 
                            new XamlXAstPropertyAssignmentNode(new XamlXAstNamePropertyReference(
                                    new XamlXAstXmlTypeReference("rootns", "Child"), "Prop1"),
                                new XamlXAstTextNode("123")),
                            // Root.AttachedProp='AttachedValue'
                            new XamlXAstPropertyAssignmentNode(new XamlXAstNamePropertyReference(
                                    new XamlXAstXmlTypeReference("rootns", "Root"), "AttachedProp"),
                                new XamlXAstTextNode("AttachedValue")),
                            //t:Namespaced.AttachedProp='AttachedValue'
                            new XamlXAstPropertyAssignmentNode(new XamlXAstNamePropertyReference(
                                    new XamlXAstXmlTypeReference("testns", "Namespaced"), "AttachedProp"),
                                new XamlXAstTextNode("AttachedValue")),
                            //x:Directive='DirectiveValue'>
                            new XamlXAstXmlDirective("directive", "Directive", new XamlXAstTextNode("DirectiveValue")),
                            // <t:SubChild Prop='321'/>
                            new XamlXAstNewInstanceNode(new XamlXAstXmlTypeReference("testns", "SubChild"))
                            {
                                Children =
                                {
                                    new XamlXAstPropertyAssignmentNode(new XamlXAstNamePropertyReference(
                                            new XamlXAstXmlTypeReference("testns", "SubChild"), "Prop"),
                                        new XamlXAstTextNode("321"))
                                }
                            },
                            //<Child.DottedProp>DottedValue</Child.DottedProp>
                            new XamlXAstPropertyAssignmentNode(new XamlXAstNamePropertyReference(
                                    new XamlXAstXmlTypeReference("rootns", "Child"), "DottedProp"),
                                new XamlXAstValueNodeList
                                {
                                    Children = {new XamlXAstTextNode("DottedValue")}
                                }),
                            // <Root.AttachedDottedProp>AttachedValue</Root.AttachedDottedProp>
                            new XamlXAstPropertyAssignmentNode(new XamlXAstNamePropertyReference(
                                    new XamlXAstXmlTypeReference("rootns", "Root"), "AttachedDottedProp"),
                                new XamlXAstValueNodeList
                                {
                                    Children = {new XamlXAstTextNode("AttachedValue")}
                                }),
                            //<Child.NodeListProp>
                            new XamlXAstPropertyAssignmentNode(new XamlXAstNamePropertyReference(
                                    new XamlXAstXmlTypeReference("rootns", "Child"), "NodeListProp"),
                                new XamlXAstValueNodeList
                                {
                                    Children =
                                    {
                                        // <SubChild/>
                                        new XamlXAstNewInstanceNode(new XamlXAstXmlTypeReference("rootns", "SubChild")),
                                        // <SubChild/>
                                        new XamlXAstNewInstanceNode(new XamlXAstXmlTypeReference("rootns", "SubChild")),
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