using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace XamlParserTests
{
    public class ParserTests
    {
        class NullLineInfo : IXamlXLineInfo
        {
            public int Line { get; set; } = 1;
            public int Position { get; set; } = 1;
        }

        [Fact]
        public void Parser_Should_Be_Able_To_Parse_A_Simple_Tree()
        {
            var root = XDocumentXamlXParser.Parse(
                @"
<Root xmlns='rootns' xmlns:t='testns' xmlns:d='directive' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Child Ext='{Extension 123, 321, Prop=test, Prop2=test2}'
        Other.Prop='{}Not extension'
        Prop1='123' 
        Root.AttachedProp='AttachedValue'
        t:Namespaced.AttachedProp='AttachedValue'
        d:Directive='DirectiveValue'
        d:DirectiveExt='{Extension 123}'>
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
            var rootType = new XamlXAstXmlTypeReference(ni, "rootns", "Root");
            var childType = new XamlXAstXmlTypeReference(ni, "rootns", "Child");
            var subChildType = new XamlXAstXmlTypeReference(ni, "rootns", "SubChild");
            var nsSubChildType = new XamlXAstXmlTypeReference(ni, "testns", "SubChild");
            var namespacedType = new XamlXAstXmlTypeReference(ni, "testns", "Namespaced");
            var extensionType = new XamlXAstXmlTypeReference(ni, "rootns", "Extension");

            var other = new XamlXDocument
            {
                NamespaceAliases = new Dictionary<string, string>
                {
                    [""] = "rootns",
                    ["t"] = "testns",
                    ["d"] = "directive",
                    ["x"] = "http://schemas.microsoft.com/winfx/2006/xaml"
                },
                Root = new XamlXAstObjectNode(ni, rootType)
                {
                    Children =
                    {
                        // <Child
                        new XamlXAstObjectNode(ni, childType)
                        {
                            Children =
                            {
                                // Ext='{Extension 123, 321, Prop=test, Prop2=test2}'
                                new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                        childType, "Ext", childType),
                                    new XamlXAstObjectNode(ni, extensionType)
                                    {
                                        Arguments =
                                        {
                                            new XamlXAstTextNode(ni, "123"),
                                            new XamlXAstTextNode(ni, "321"),
                                        },
                                        Children =
                                        {
                                            new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                                    extensionType, "Prop", extensionType),
                                                new XamlXAstTextNode(ni, "test")),
                                            new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                                    extensionType, "Prop2", extensionType),
                                                new XamlXAstTextNode(ni, "test2")),
                                        }
                                    }),                             
                                //Other.Prop='{}Not extension'
                                new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                        new XamlXAstXmlTypeReference(ni, "rootns", "Other"), "Prop", childType),
                                    new XamlXAstTextNode(ni, "Not extension")),
                                //  Prop1='123' 
                                new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                        childType, "Prop1", childType),
                                    new XamlXAstTextNode(ni, "123")),
                                // Root.AttachedProp='AttachedValue'
                                new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                        rootType, "AttachedProp", childType),
                                    new XamlXAstTextNode(ni, "AttachedValue")),
                                //t:Namespaced.AttachedProp='AttachedValue'
                                new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                        namespacedType, "AttachedProp", childType),
                                    new XamlXAstTextNode(ni, "AttachedValue")),
                                //d:Directive='DirectiveValue'>
                                new XamlXAstXmlDirective(ni, "directive", "Directive", new[]
                                {
                                    new XamlXAstTextNode(ni, "DirectiveValue")
                                }),
                                //d:DirectiveExt='{Extension 123}'>
                                new XamlXAstXmlDirective(ni, "directive", "DirectiveExt", new[]
                                {
                                    new XamlXAstObjectNode(ni, extensionType)
                                    {
                                        Arguments =
                                        {
                                            new XamlXAstTextNode(ni, "123"),
                                        }
                                    }
                                }),
                                // <t:SubChild Prop='321'/>
                                new XamlXAstObjectNode(ni, nsSubChildType)
                                {
                                    Children =
                                    {
                                        new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                                nsSubChildType, "Prop", nsSubChildType),
                                            new XamlXAstTextNode(ni, "321"))
                                    }
                                },
                                //<Child.DottedProp>DottedValue</Child.DottedProp>
                                new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                        childType, "DottedProp", childType),
                                    new[]
                                    {
                                        new XamlXAstTextNode(ni, "DottedValue")
                                    }),
                                // <Root.AttachedDottedProp>AttachedValue</Root.AttachedDottedProp>
                                new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                        rootType, "AttachedDottedProp", childType),
                                    new[]
                                    {
                                        new XamlXAstTextNode(ni, "AttachedValue")
                                    }),
                                //<Child.NodeListProp>
                                new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                        childType, "NodeListProp", childType),
                                    new[]
                                    {
                                        // <SubChild/>
                                        new XamlXAstObjectNode(ni, subChildType),
                                        // <SubChild/>
                                        new XamlXAstObjectNode(ni, subChildType),
                                    })
                            }
                        },
                        //<GenericType x:TypeArguments='Child,t:NamespacedGeneric(Child,GenericType(Child, t:Namespaced))'/>
                        new XamlXAstObjectNode(ni, new XamlXAstXmlTypeReference(ni, "rootns", "GenericType",
                            new[]
                            {
                                childType,
                                new XamlXAstXmlTypeReference(ni, "testns", "NamespacedGeneric", new[]
                                {
                                    childType,
                                    new XamlXAstXmlTypeReference(ni, "rootns", "GenericType", new[]
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

        [Theory, InlineData(false), InlineData(true)]
        public void Parser_Should_Handle_Ignorable_Content(bool map)
        {
            var root = XDocumentXamlXParser.Parse(@"
<Root xmlns='rootns' xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' 
    mc:Ignorable='d d2' xmlns:d='test' xmlns:d2='test2'
    d:DataContext='123' d2:Lalala='321'>
    <d:DesignWidth>test</d:DesignWidth>
</Root>
 ", map ? new Dictionary<string, string>() {["test"] = "mapped"} : null);
            var ni = new NullLineInfo();
            var rootType = new XamlXAstXmlTypeReference(ni, "rootns", "Root");

            if (map)
            {
                Helpers.StructDiff(root.Root, new XamlXAstObjectNode(ni, rootType)
                {
                    Children =
                    {
                        new XamlXAstXmlDirective(ni, "mapped", "DataContext", new[] {new XamlXAstTextNode(ni, "123"),}),
                        new XamlXAstObjectNode(ni, new XamlXAstXmlTypeReference(ni, "mapped", "DesignWidth"))
                        {
                            Children =
                            {
                                new XamlXAstTextNode(ni, "test")
                            }
                        }
                    }
                });
            }
            else
                Helpers.StructDiff(root.Root, new XamlXAstObjectNode(ni, rootType)
                {

                });
        }
    }
}