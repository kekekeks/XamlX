using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using Xunit;

namespace XamlParserTests
{
    public sealed class ResolveContentPropertyTransformerTest
    {
        [Fact]
        public void TransformDoesNotFailForObjectWithWhitespaces()
        {
            // <Object>\n\t\n\t\n\t</Object>

            var doc = XDocumentXamlParser.Parse("<root/>");
            var typeSystem = CreateTypeSystem();
            var context = CreateContext(typeSystem, doc);

            var objectType = typeSystem.GetType("System.Object");

            var node = new XamlAstObjectNode(doc.Root, new XamlAstClrTypeReference(doc.Root, objectType, false));
            node.Children.Add(new XamlAstTextNode(doc.Root, "\n\t"));
            node.Children.Add(new XamlAstTextNode(doc.Root, "\n\t"));
            node.Children.Add(new XamlAstTextNode(doc.Root, "\n\t"));

            var transformer = new ResolveContentPropertyTransformer();
            transformer.Transform(context, node);

            Assert.True(true);
        }

        [Fact]
        public void TransformFailForObjectWithContent()
        {
            // <Object><Int32>1<Int32></Object>

            var doc = XDocumentXamlParser.Parse("<root/>");
            var typeSystem = CreateTypeSystem();
            var context = CreateContext(typeSystem, doc);

            var objectType = typeSystem.GetType("System.Object");
            var intType = typeSystem.GetType("System.Int32");

            var node = new XamlAstObjectNode(doc.Root, new XamlAstClrTypeReference(doc.Root, objectType, false));
            node.Children.Add(new XamlConstantNode(doc.Root, intType, 1));

            var transformer = new ResolveContentPropertyTransformer();
            var ex = Assert.Throws<XamlTransformException>(() => transformer.Transform(context, node));
        }

        [Fact]
        public void TransformFailForObjectWithContentAndWhitespaces()
        {
            // <Object>\n\t<Int32>1<Int32>\n\t</Object>

            var doc = XDocumentXamlParser.Parse("<root/>");
            var typeSystem = CreateTypeSystem();
            var context = CreateContext(typeSystem, doc);

            var objectType = typeSystem.GetType("System.Object");
            var intType = typeSystem.GetType("System.Int32");

            var node = new XamlAstObjectNode(doc.Root, new XamlAstClrTypeReference(doc.Root, objectType, false));
            node.Children.Add(new XamlAstTextNode(doc.Root, "\n\t"));
            node.Children.Add(new XamlConstantNode(doc.Root, intType, 1));
            node.Children.Add(new XamlAstTextNode(doc.Root, "\n\t"));

            var transformer = new ResolveContentPropertyTransformer();
            var ex = Assert.Throws<XamlTransformException>(() => transformer.Transform(context, node));
        }

        [Fact]
        public void TransformForObjectWithContentAndContentProperty()
        {
            // <ObjectWithContent><Int32>1<Int32></ObjectWithContent>

            var doc = XDocumentXamlParser.Parse("<root/>");
            var typeSystem = CreateTypeSystem();
            var context = CreateContext(typeSystem, doc);

            var objectWithContentType = typeSystem.GetType(typeof(ObjectWithContent).FullName);
            var intType = typeSystem.GetType("System.Int32");

            var node = new XamlAstObjectNode(doc.Root, new XamlAstClrTypeReference(doc.Root, objectWithContentType, false));
            var value1 = new XamlConstantNode(doc.Root, intType, 1);
            node.Children.Add(value1);

            var transformer = new ResolveContentPropertyTransformer();
            transformer.Transform(context, node);

            var child = Assert.Single(node.Children);
            var content = Assert.IsType<XamlAstXamlPropertyValueNode>(child);

            var value = Assert.Single(content.Values);
            Assert.Same(value1, value);

            var property = Assert.IsType<XamlAstClrProperty>(content.Property);
            Assert.Equal(nameof(ObjectWithContent.ChildContent), property.Name);
            var setter = Assert.Single(property.Setters);
            Assert.False(setter.BinderParameters.AllowMultiple);
        }

        [Fact]
        public void TransformForObjectWithContentAndWhitespacesAndContentProperty()
        {
            // <ObjectWithContent>\n\t<Int32>1<Int32>\n\t</ObjectWithContent>

            var doc = XDocumentXamlParser.Parse("<root/>");
            var typeSystem = CreateTypeSystem();
            var context = CreateContext(typeSystem, doc);

            var objectWithContentType = typeSystem.GetType(typeof(ObjectWithContent).FullName);
            var intType = typeSystem.GetType("System.Int32");

            var node = new XamlAstObjectNode(doc.Root, new XamlAstClrTypeReference(doc.Root, objectWithContentType, false));
            var spaces1 = new XamlAstTextNode(doc.Root, "\n\t");
            var value1 = new XamlConstantNode(doc.Root, intType, 1);
            var spaces2 = new XamlAstTextNode(doc.Root, "\n\t");
            node.Children.AddRange(new IXamlAstNode[] { spaces1, value1, spaces2 });

            var transformer = new ResolveContentPropertyTransformer();
            transformer.Transform(context, node);

            var child = Assert.Single(node.Children);
            var content = Assert.IsType<XamlAstXamlPropertyValueNode>(child);

            Assert.Collection(
                content.Values,
                v => Assert.Same(spaces1, v),
                v => Assert.Same(value1, v),
                v => Assert.Same(spaces2, v));

            var property = Assert.IsType<XamlAstClrProperty>(content.Property);
            Assert.Equal(nameof(ObjectWithContent.ChildContent), property.Name);
            var setter = Assert.Single(property.Setters);
            Assert.False(setter.BinderParameters.AllowMultiple);
        }

        [Fact]
        public void TransformForObjectWithPropertyValueAndContentWithSpaces()
        {
            // <ObjectWithContent>\n\t<ObjectWithContent.Id>123<ObjectWithContent.Id>\n\t<Int32>1<Int32>\n\t</ObjectWithContent>

            var doc = XDocumentXamlParser.Parse("<root/>");
            var typeSystem = CreateTypeSystem();
            var context = CreateContext(typeSystem, doc);

            var objectWithContentType = typeSystem.GetType(typeof(ObjectWithContent).FullName);
            var intType = typeSystem.GetType("System.Int32");

            var idProperty = objectWithContentType.Properties.First(p => p.Name == "Id");

            var node = new XamlAstObjectNode(doc.Root, new XamlAstClrTypeReference(doc.Root, objectWithContentType, false));

            var spaces1 = new XamlAstTextNode(doc.Root, "\n\t");
            var id = new XamlAstXamlPropertyValueNode(doc.Root, new XamlAstClrProperty(doc.Root, idProperty, context.Configuration), new XamlConstantNode(doc.Root, intType, 123), false);
            var spaces2 = new XamlAstTextNode(doc.Root, "\n\t");
            var value = new XamlConstantNode(doc.Root, intType, 456);
            var spaces3 = new XamlAstTextNode(doc.Root, "\n\t");
            node.Children.AddRange(new IXamlAstNode[] { spaces1, id, spaces2, value, spaces3 });

            var transformer = new ResolveContentPropertyTransformer();
            transformer.Transform(context, node);

            Assert.Collection(
                node.Children,
                c => Assert.Same(id, c),
                c =>
                {
                    var content = Assert.IsType<XamlAstXamlPropertyValueNode>(c);
                    Assert.Collection(
                        content.Values,
                        v => Assert.Same(spaces1, v),
                        v => Assert.Same(spaces2, v),
                        v =>
                        {
                            Assert.Same(value, v);
                            var property = Assert.IsType<XamlAstClrProperty>(content.Property);
                            Assert.Equal(nameof(ObjectWithContent.ChildContent), property.Name);
                            var setter = Assert.Single(property.Setters);
                            Assert.False(setter.BinderParameters.AllowMultiple);
                        },
                        v => Assert.Same(spaces3, v));
                });
        }

        [Fact]
        public void TransformForArrayListWithContent()
        {
            // <ArrayList><Int32>1<Int32><Int32>2<Int32><Int32>3<Int32></ArrayList>

            var doc = XDocumentXamlParser.Parse("<root/>");
            var typeSystem = CreateTypeSystem();
            var context = CreateContext(typeSystem, doc);

            var arrayListType = typeSystem.GetType("System.Collections.ArrayList");
            var intType = typeSystem.GetType("System.Int32");

            var node = new XamlAstObjectNode(doc.Root, new XamlAstClrTypeReference(doc.Root, arrayListType, false));
            var value1 = new XamlConstantNode(doc.Root, intType, 1);
            var value2 = new XamlConstantNode(doc.Root, intType, 2);
            var value3 = new XamlConstantNode(doc.Root, intType, 3);
            node.Children.AddRange(new[] { value1, value2, value3 });

            var transformer = new ResolveContentPropertyTransformer();
            transformer.Transform(context, node);

            var child = Assert.Single(node.Children);
            var content = Assert.IsType<XamlAstXamlPropertyValueNode>(child);

            Assert.Collection(
                content.Values,
                v => Assert.Same(value1, v),
                v => Assert.Same(value2, v),
                v => Assert.Same(value3, v));

            var property = Assert.IsType<XamlAstClrProperty>(content.Property);
            Assert.Equal("Content", property.Name);
            Assert.All(
                property.Setters,
                s =>
                {
                    var setter = Assert.IsType<XamlDirectCallPropertySetter>(s);
                    Assert.True(setter.BinderParameters.AllowMultiple);
                });
        }

        private IXamlTypeSystem CreateTypeSystem()
        {
            return new CecilTypeSystem(new[]
            {
                GetType().Assembly.Location,
                typeof(void).Assembly.Location,
                typeof(ContentAttribute).Assembly.Location,
            });
        }

        private AstTransformationContext CreateContext(IXamlTypeSystem typeSystem, XamlDocument doc)
        {
            var thisAssembly = typeSystem.FindAssembly(typeof(ResolveContentPropertyTransformerTest).Assembly.GetName().Name);
            var mapping = new XamlLanguageTypeMappings(typeSystem, false)
            {
                ContentAttributes =
                {
                    typeSystem.GetType(typeof(ContentAttribute).FullName)
                }
            };

            var compilerConfig = new TransformerConfiguration(typeSystem, thisAssembly, mapping);
            return new AstTransformationContext(compilerConfig, doc);
        }
    }
}
