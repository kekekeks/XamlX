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
        class NullLineInfo : IXamlLineInfo
        {
            public int Line { get; set; } = 1;
            public int Position { get; set; } = 1;
        }

        [Fact]
        public void Parser_Should_Be_Able_To_Parse_A_Simple_Tree()
        {
            var root = XDocumentXamlParser.Parse(
                @"
<Root xmlns='rootns' xmlns:t='testns' xmlns:d='directive' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Child Ext='{Extension 123, 321, Prop=test, Prop2=test2, Prop3={Extension}, Prop4=test3}'
        Other.Prop='{}Not extension'
        Prop1='123' 
        Root.AttachedProp='AttachedValue'
        t:Namespaced.AttachedProp='AttachedValue'
        d:Directive='DirectiveValue'
        d:DirectiveExt='{Extension 123}'>
        <t:SubChild Prop='321' Root.AttachedProp='AttachedValue'/>
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
            var rootType = new XamlAstXmlTypeReference(ni, "rootns", "Root");
            var childType = new XamlAstXmlTypeReference(ni, "rootns", "Child");
            var subChildType = new XamlAstXmlTypeReference(ni, "rootns", "SubChild");
            var nsSubChildType = new XamlAstXmlTypeReference(ni, "testns", "SubChild");
            var namespacedType = new XamlAstXmlTypeReference(ni, "testns", "Namespaced");
            var extensionType = new XamlAstXmlTypeReference(ni, "rootns", "Extension")
            {
                IsMarkupExtension = true
            };

            var other = new XamlDocument
            {
                NamespaceAliases = new Dictionary<string, string>
                {
                    [""] = "rootns",
                    ["t"] = "testns",
                    ["d"] = "directive",
                    ["x"] = "http://schemas.microsoft.com/winfx/2006/xaml"
                },
                Root = new XamlAstObjectNode(ni, rootType)
                {
                    Children =
                    {
                        // <Child
                        new XamlAstObjectNode(ni, childType)
                        {
                            Children =
                            {
                                // Ext='{Extension 123, 321, Prop=test, Prop2=test2}'
                                new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                        childType, "Ext", childType),
                                    new XamlAstObjectNode(ni, extensionType)
                                    {
                                        Arguments =
                                        {
                                            new XamlAstTextNode(ni, "123"),
                                            new XamlAstTextNode(ni, "321"),
                                        },
                                        Children =
                                        {
                                            new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                                    extensionType, "Prop", extensionType),
                                                new XamlAstTextNode(ni, "test")),
                                            new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                                    extensionType, "Prop2", extensionType),
                                                new XamlAstTextNode(ni, "test2")),
                                            new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                                    extensionType, "Prop3", extensionType),
                                                new XamlAstObjectNode(ni, extensionType)),
                                            new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                                    extensionType, "Prop4", extensionType),
                                                new XamlAstTextNode(ni, "test3")),
                                        }
                                    }),                             
                                //Other.Prop='{}Not extension'
                                new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                        new XamlAstXmlTypeReference(ni, "rootns", "Other"), "Prop", childType),
                                    new XamlAstTextNode(ni, "Not extension")),
                                //  Prop1='123' 
                                new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                        childType, "Prop1", childType),
                                    new XamlAstTextNode(ni, "123")),
                                // Root.AttachedProp='AttachedValue'
                                new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                        rootType, "AttachedProp", childType),
                                    new XamlAstTextNode(ni, "AttachedValue")),
                                //t:Namespaced.AttachedProp='AttachedValue'
                                new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                        namespacedType, "AttachedProp", childType),
                                    new XamlAstTextNode(ni, "AttachedValue")),
                                //d:Directive='DirectiveValue'>
                                new XamlAstXmlDirective(ni, "directive", "Directive", new[]
                                {
                                    new XamlAstTextNode(ni, "DirectiveValue")
                                }),
                                //d:DirectiveExt='{Extension 123}'>
                                new XamlAstXmlDirective(ni, "directive", "DirectiveExt", new[]
                                {
                                    new XamlAstObjectNode(ni, extensionType)
                                    {
                                        Arguments =
                                        {
                                            new XamlAstTextNode(ni, "123"),
                                        }
                                    }
                                }),
                                // <t:SubChild Prop='321' Root.AttachedProp='AttachedValue'/>
                                new XamlAstObjectNode(ni, nsSubChildType)
                                {
                                    Children =
                                    {
                                        new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                                nsSubChildType, "Prop", nsSubChildType),
                                            new XamlAstTextNode(ni, "321")),
                                        // Root.AttachedProp='AttachedValue'
                                        new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                                rootType, "AttachedProp", nsSubChildType),
                                            new XamlAstTextNode(ni, "AttachedValue")),
                                    }
                                },
                                //<Child.DottedProp>DottedValue</Child.DottedProp>
                                new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                        childType, "DottedProp", childType),
                                    new[]
                                    {
                                        new XamlAstTextNode(ni, "DottedValue")
                                    }),
                                // <Root.AttachedDottedProp>AttachedValue</Root.AttachedDottedProp>
                                new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                        rootType, "AttachedDottedProp", childType),
                                    new[]
                                    {
                                        new XamlAstTextNode(ni, "AttachedValue")
                                    }),
                                //<Child.NodeListProp>
                                new XamlAstXamlPropertyValueNode(ni, new XamlAstNamePropertyReference(ni,
                                        childType, "NodeListProp", childType),
                                    new[]
                                    {
                                        // <SubChild/>
                                        new XamlAstObjectNode(ni, subChildType),
                                        // <SubChild/>
                                        new XamlAstObjectNode(ni, subChildType),
                                    })
                            }
                        },
                        //<GenericType x:TypeArguments='Child,t:NamespacedGeneric(Child,GenericType(Child, t:Namespaced))'/>
                        new XamlAstObjectNode(ni, new XamlAstXmlTypeReference(ni, "rootns", "GenericType",
                            new[]
                            {
                                childType,
                                new XamlAstXmlTypeReference(ni, "testns", "NamespacedGeneric", new[]
                                {
                                    childType,
                                    new XamlAstXmlTypeReference(ni, "rootns", "GenericType", new[]
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
            var root = XDocumentXamlParser.Parse(@"
<Root xmlns='rootns' xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006' 
    mc:Ignorable='d d2' xmlns:d='test' xmlns:d2='test2'
    d:DataContext='123' d2:Lalala='321'>
    <d:DesignWidth>test</d:DesignWidth>
</Root>
 ", map ? new Dictionary<string, string>() {["test"] = "mapped"} : null);
            var ni = new NullLineInfo();
            var rootType = new XamlAstXmlTypeReference(ni, "rootns", "Root");

            if (map)
            {
                Helpers.StructDiff(root.Root, new XamlAstObjectNode(ni, rootType)
                {
                    Children =
                    {
                        new XamlAstXmlDirective(ni, "mapped", "DataContext", new[] {new XamlAstTextNode(ni, "123"),}),
                        new XamlAstObjectNode(ni, new XamlAstXmlTypeReference(ni, "mapped", "DesignWidth"))
                        {
                            Children =
                            {
                                new XamlAstTextNode(ni, "test")
                            }
                        }
                    }
                });
            }
            else
                Helpers.StructDiff(root.Root, new XamlAstObjectNode(ni, rootType)
                {

                });
        }

        [Fact]
        public void Empty_Extension_With_Space_Should_Be_Parsed()
        {
            var ni = new NullLineInfo();
            var parsed = XamlMarkupExtensionParser.Parse(ni, "{Binding }",
                n => new XamlAstXmlTypeReference(ni, "", n));
            Helpers.StructDiff(parsed, new XamlAstObjectNode(new NullLineInfo(),
                new XamlAstXmlTypeReference(ni, "", "Binding")
                {
                    IsMarkupExtension = true
                }));
        }
    }
}