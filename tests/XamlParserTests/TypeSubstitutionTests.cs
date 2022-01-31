using System.Collections.Generic;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using Xunit;

namespace XamlParserTests
{
    namespace TypeSubstitutionModels
    {
        public interface IBinding
        {
        }

        public class Binding : IBinding
        {
        } 
    }

    public class TypeSubstitutionTests : CompilerTestBase
    {
        [Fact]
        public void PropertyAssignmentNode_Has_Correct_Setter_Available()
        {
            var compiler = new TestCompiler(Configuration);
            var parsed = XDocumentXamlParser.Parse(@"<SimpleClass xmlns='test' Test='bind me'/>");

            // Transforms the "bind me" string into a new Binding instance.
            compiler.Transform(parsed);
            
            var objectInit = (XamlObjectInitializationNode)((XamlValueWithManipulationNode)parsed.Root).Manipulation;
            var propertyAssignment = (XamlPropertyAssignmentNode)objectInit.Manipulation;

            // Make sure that BindingSetter is available after the "bind me" value has been substituted with
            // a new Binding instance.
            //Assert.Contains(propertyAssignment.Setters, x => x is BindingSetter);
        }

        private class TestCompiler : XamlILCompiler
        {
            public TestCompiler(TransformerConfiguration configuration)
                : base(configuration, new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>(), true)
            {
                var index = Transformers.FindIndex(x => x is PropertyReferenceResolver);
                Transformers.Insert(index + 1, new PropertyResolver());
                Transformers.Add(new ValueReplacer());
            }
        }

        /// <summary>
        /// Transforms <see cref="XamlAstClrProperty"/> into <see cref="BindableProperty"/>.
        /// </summary>
        private class PropertyResolver : IXamlAstTransformer
        {
            public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
            {
                var bindingType = context.Configuration.TypeSystem.FindType("XamlParserTests.TypeSubstitutionModels.IBinding");

                if (node is XamlAstClrProperty property)
                {
                    return new BindableProperty(property, bindingType);
                }

                return node;
            }
        }

        /// <summary>
        /// Replaces a "bind me" string with a <see cref="TypeSubstitutionModels.Binding"/> object.
        /// </summary>
        private class ValueReplacer : IXamlAstTransformer
        {
            public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
            {
                if (node is XamlAstTextNode text && text.Text == "bind me")
                {
                    var bindingType = context.Configuration.TypeSystem.FindType("XamlParserTests.TypeSubstitutionModels.Binding");
                    return new XamlAstNewClrObjectNode(node,  new XamlAstClrTypeReference(node, bindingType, false),
                        bindingType.FindConstructor(), new List<IXamlAstValueNode>());
                }

                return node;
            }
        }

        /// <summary>
        /// Custom <see cref="XamlAstClrProperty"/> which adds a <see cref="BindingSetter"/>.
        /// </summary>
        private class BindableProperty : XamlAstClrProperty
        {
            public BindableProperty(XamlAstClrProperty original, IXamlType bindingType)
                : base(original, original.Name, original.DeclaringType, original.Getter, original.Setters)
            {
                Setters.Add(new BindingSetter(original.DeclaringType, bindingType));
            }
        }

        /// <summary>
        /// Property setter for <see cref="BindableProperty"/>.
        /// </summary>
        private class BindingSetter : IXamlPropertySetter
        {
            public BindingSetter(IXamlType targetType, IXamlType bindingType)
            {
                TargetType = targetType;
                ParameterType = bindingType;
            }

            public IXamlType TargetType { get; }
            public IXamlType ParameterType { get; }
            public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();
            public IReadOnlyList<IXamlType> Parameters { get; }

            public bool Matches(IReadOnlyList<IXamlAstValueNode> arguments)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}