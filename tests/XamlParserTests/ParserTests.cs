using System;
using System.Collections.Generic;
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
    <Child 
        Other.Prop='{TestExt something}'
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
            var rootType = new XamlXAstXmlTypeReference(ni, "rootns", "Root");
            var childType = new XamlXAstXmlTypeReference(ni, "rootns", "Child");
            var subChildType = new XamlXAstXmlTypeReference(ni, "rootns", "SubChild");
            var nsSubChildType = new XamlXAstXmlTypeReference(ni, "testns", "SubChild");
            var namespacedType = new XamlXAstXmlTypeReference(ni, "testns", "Namespaced");

            var other = new XamlXAstRootInstanceNode(ni, rootType)
            {
                XmlNamespaces = new Dictionary<string, string>
                {
                    [""] = "rootns",
                    ["t"] = "testns",
                    ["d"] = "directive",
                    ["x"] = "http://schemas.microsoft.com/winfx/2006/xaml"
                },
                Children =
                {
                    // <Child
                    new XamlXAstNewInstanceNode(ni, childType)
                    {
                        Children =
                        {
                            // Other.Prop='{TestExt something}'
                            new XamlXAstXamlPropertyValueNode(ni, new XamlXAstNamePropertyReference(ni,
                                    new XamlXAstXmlTypeReference(ni, "rootns", "Other"), "Prop", childType),
                                new XamlXAstTextNode(ni, "{TestExt something}")),
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
                            // <t:SubChild Prop='321'/>
                            new XamlXAstNewInstanceNode(ni, nsSubChildType)
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
                                    new XamlXAstNewInstanceNode(ni, subChildType),
                                    // <SubChild/>
                                    new XamlXAstNewInstanceNode(ni, subChildType),
                                })
                        }
                    },
                    //<GenericType x:TypeArguments='Child,t:NamespacedGeneric(Child,GenericType(Child, t:Namespaced))'/>
                    new XamlXAstNewInstanceNode(ni, new XamlXAstXmlTypeReference(ni, "rootns", "GenericType",
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
            };
            Helpers.StructDiff(root, other);

        }
    }
}