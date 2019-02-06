using System;
using System.Collections.Generic;
using System.Xml.Linq;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Parsers;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace XamlParserTests
{
    public class ParserTests
    {
        class NullLineInfo : IXamlIlLineInfo
        {
            public int Line { get; set; } = 1;
            public int Position { get; set; } = 1;
        }

        [Fact]
        public void Parser_Should_Be_Able_To_Parse_A_Simple_Tree()
        {
            var root = XDocumentXamlIlParser.Parse(
                @"
<Root xmlns='rootns' xmlns:t='testns' xmlns:d='directive' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Child Ext='{Extension 123, 321, Prop=test, Prop2=test2}'
        Other.Prop='{}Not extension'
        Prop1='123' 
        Root.AttachedProp='AttachedValue'
        t:Namespaced.AttachedProp='AttachedValue'
        d:Directive='DirectiveValue'>
        <t:SubChild Prop='321'/>
        <Child.DottedProp>DottedValue</Child.DottedProp>
        <Root.AttachedDottedProp>AttachedValue</Root.AttachedDottedProp>
        <Child.NodeListProp>
            <SubChild/>
            <SubChild/>
        </Child.NodeListProp>
    </Child>
    <GenericType x:TypeArguments='Child,t:NamespacedGeneric(Child,GenericType ( Child, t:Namespaced) )'/>

</Root>");
            var ni = new NullLineInfo();
            var rootType = new XamlIlAstXmlTypeReference(ni, "rootns", "Root");
            var childType = new XamlIlAstXmlTypeReference(ni, "rootns", "Child");
            var subChildType = new XamlIlAstXmlTypeReference(ni, "rootns", "SubChild");
            var nsSubChildType = new XamlIlAstXmlTypeReference(ni, "testns", "SubChild");
            var namespacedType = new XamlIlAstXmlTypeReference(ni, "testns", "Namespaced");
            var extensionType = new XamlIlAstXmlTypeReference(ni, "rootns", "Extension");

            var other = new XamlIlDocument
            {
                NamespaceAliases = new Dictionary<string, string>
                {
                    [""] = "rootns",
                    ["t"] = "testns",
                    ["d"] = "directive",
                    ["x"] = "http://schemas.microsoft.com/winfx/2006/xaml"
                },
                Root = new XamlIlAstObjectNode(ni, rootType)
                {
                    Children =
                    {
                        // <Child
                        new XamlIlAstObjectNode(ni, childType)
                        {
                            Children =
                            {
                                // Ext='{Extension 123, 321, Prop=test, Prop2=test2}'
                                new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                        childType, "Ext", childType),
                                    new XamlIlAstObjectNode(ni, extensionType)
                                    {
                                        Arguments =
                                        {
                                            new XamlIlAstTextNode(ni, "123"),
                                            new XamlIlAstTextNode(ni, "321"),
                                        },
                                        Children =
                                        {
                                            new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                                    extensionType, "Prop", extensionType),
                                                new XamlIlAstTextNode(ni, "test")),
                                            new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                                    extensionType, "Prop2", extensionType),
                                                new XamlIlAstTextNode(ni, "test2")),
                                        }
                                    }),                             
                                //Other.Prop='{}Not extension'
                                new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                        new XamlIlAstXmlTypeReference(ni, "rootns", "Other"), "Prop", childType),
                                    new XamlIlAstTextNode(ni, "Not extension")),
                                //  Prop1='123' 
                                new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                        childType, "Prop1", childType),
                                    new XamlIlAstTextNode(ni, "123")),
                                // Root.AttachedProp='AttachedValue'
                                new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                        rootType, "AttachedProp", childType),
                                    new XamlIlAstTextNode(ni, "AttachedValue")),
                                //t:Namespaced.AttachedProp='AttachedValue'
                                new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                        namespacedType, "AttachedProp", childType),
                                    new XamlIlAstTextNode(ni, "AttachedValue")),
                                //d:Directive='DirectiveValue'>
                                new XamlIlAstXmlDirective(ni, "directive", "Directive", new[]
                                {
                                    new XamlIlAstTextNode(ni, "DirectiveValue")
                                }),
                                // <t:SubChild Prop='321'/>
                                new XamlIlAstObjectNode(ni, nsSubChildType)
                                {
                                    Children =
                                    {
                                        new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                                nsSubChildType, "Prop", nsSubChildType),
                                            new XamlIlAstTextNode(ni, "321"))
                                    }
                                },
                                //<Child.DottedProp>DottedValue</Child.DottedProp>
                                new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                        childType, "DottedProp", childType),
                                    new[]
                                    {
                                        new XamlIlAstTextNode(ni, "DottedValue")
                                    }),
                                // <Root.AttachedDottedProp>AttachedValue</Root.AttachedDottedProp>
                                new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                        rootType, "AttachedDottedProp", childType),
                                    new[]
                                    {
                                        new XamlIlAstTextNode(ni, "AttachedValue")
                                    }),
                                //<Child.NodeListProp>
                                new XamlIlAstXamlPropertyValueNode(ni, new XamlIlAstNamePropertyReference(ni,
                                        childType, "NodeListProp", childType),
                                    new[]
                                    {
                                        // <SubChild/>
                                        new XamlIlAstObjectNode(ni, subChildType),
                                        // <SubChild/>
                                        new XamlIlAstObjectNode(ni, subChildType),
                                    })
                            }
                        },
                        //<GenericType x:TypeArguments='Child,t:NamespacedGeneric(Child,GenericType(Child, t:Namespaced))'/>
                        new XamlIlAstObjectNode(ni, new XamlIlAstXmlTypeReference(ni, "rootns", "GenericType",
                            new[]
                            {
                                childType,
                                new XamlIlAstXmlTypeReference(ni, "testns", "NamespacedGeneric", new[]
                                {
                                    childType,
                                    new XamlIlAstXmlTypeReference(ni, "rootns", "GenericType", new[]
                                    {
                                        childType,
                                        namespacedType
                                    }),
                                }),
                            }))
                    }
                }
            };
            Helpers.StructDiff(root, other);

        }
    }
}