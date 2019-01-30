using System;
using System.Collections.Generic;
using System.Xml.Linq;
using XamlIl;
using XamlIl.Parsers;
using Xunit;

namespace XamlParserTests
{
    public class BasicTests
    {
        [Fact]
        public void Test1()
        {
            var root = XDocumentXamlIlParser.Parse(
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
            var other = new XamlIlAstRootInstanceNode(new XamlIlAstXmlTypeReference("rootns", "Root"))
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
                    new XamlIlAstNewInstanceNode(new XamlIlAstXmlTypeReference("rootns", "Child"))
                    {
                        Children =
                        {
                            // Other.Prop='{TestExt something}'
                            new XamlIlAstPropertyAssignmentNode(new XamlIlAstNamePropertyReference(
                                    new XamlIlAstXmlTypeReference("rootns", "Other"), "Prop"),
                                new XamlIlAstTextNode("{TestExt something}")),
                            //  Prop1='123' 
                            new XamlIlAstPropertyAssignmentNode(new XamlIlAstNamePropertyReference(
                                    new XamlIlAstXmlTypeReference("rootns", "Child"), "Prop1"),
                                new XamlIlAstTextNode("123")),
                            // Root.AttachedProp='AttachedValue'
                            new XamlIlAstPropertyAssignmentNode(new XamlIlAstNamePropertyReference(
                                    new XamlIlAstXmlTypeReference("rootns", "Root"), "AttachedProp"),
                                new XamlIlAstTextNode("AttachedValue")),
                            //t:Namespaced.AttachedProp='AttachedValue'
                            new XamlIlAstPropertyAssignmentNode(new XamlIlAstNamePropertyReference(
                                    new XamlIlAstXmlTypeReference("testns", "Namespaced"), "AttachedProp"),
                                new XamlIlAstTextNode("AttachedValue")),
                            //x:Directive='DirectiveValue'>
                            new XamlIlAstXmlDirective("directive", "Directive", new XamlIlAstTextNode("DirectiveValue")),
                            // <t:SubChild Prop='321'/>
                            new XamlIlAstNewInstanceNode(new XamlIlAstXmlTypeReference("testns", "SubChild"))
                            {
                                Children =
                                {
                                    new XamlIlAstPropertyAssignmentNode(new XamlIlAstNamePropertyReference(
                                            new XamlIlAstXmlTypeReference("testns", "SubChild"), "Prop"),
                                        new XamlIlAstTextNode("321"))
                                }
                            },
                            //<Child.DottedProp>DottedValue</Child.DottedProp>
                            new XamlIlAstPropertyAssignmentNode(new XamlIlAstNamePropertyReference(
                                    new XamlIlAstXmlTypeReference("rootns", "Child"), "DottedProp"),
                                new XamlIlAstValueNodeList
                                {
                                    Children = {new XamlIlAstTextNode("DottedValue")}
                                }),
                            // <Root.AttachedDottedProp>AttachedValue</Root.AttachedDottedProp>
                            new XamlIlAstPropertyAssignmentNode(new XamlIlAstNamePropertyReference(
                                    new XamlIlAstXmlTypeReference("rootns", "Root"), "AttachedDottedProp"),
                                new XamlIlAstValueNodeList
                                {
                                    Children = {new XamlIlAstTextNode("AttachedValue")}
                                }),
                            //<Child.NodeListProp>
                            new XamlIlAstPropertyAssignmentNode(new XamlIlAstNamePropertyReference(
                                    new XamlIlAstXmlTypeReference("rootns", "Child"), "NodeListProp"),
                                new XamlIlAstValueNodeList
                                {
                                    Children =
                                    {
                                        // <SubChild/>
                                        new XamlIlAstNewInstanceNode(new XamlIlAstXmlTypeReference("rootns", "SubChild")),
                                        // <SubChild/>
                                        new XamlIlAstNewInstanceNode(new XamlIlAstXmlTypeReference("rootns", "SubChild")),
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