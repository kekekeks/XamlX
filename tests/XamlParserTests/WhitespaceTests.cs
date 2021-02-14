using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace XamlParserTests
{
    // Most of these test cases are derived from these sources:
    // https://docs.microsoft.com/en-us/dotnet/desktop/xaml-services/white-space-processing
    // https://www.w3.org/TR/2006/REC-xml11-20060816/
    public class WhitespaceTests : CompilerTestBase
    {
        private const string Ns = "xmlns='test'";

        // As per the XAML docs, these three characters are considered whitespace
        private const string AllWhitespace = " \n\t";

        private readonly ITestOutputHelper _output;

        public WhitespaceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void EmptyTagResultsInNullContent()
        {
            var content = TestContentControlContent("");
            Assert.Null(content);
        }

        [Fact]
        public void SelfClosingTagResultsInNullContent()
        {
            var control = ReadXaml<ContentControl>($"<ContentControl {Ns}/>");
            Assert.Null(control.Content);
        }

        [Fact]
        public void WhiteSpaceOnlyContentResultsInNullContentWithoutOptIn()
        {
            var content = TestContentControlContent(AllWhitespace);
            Assert.Null(content);
        }

        [Fact]
        public void LeadingAndTrailingWhiteSpaceIsTrimmedWithoutOptIn()
        {
            var content = TestContentControlContent($"{AllWhitespace}ABC{AllWhitespace}");
            Assert.Equal("ABC", content);
        }

        [Fact]
        public void InnerWhitespaceIsPreservedButCollapsed()
        {
            var content = TestContentControlContent($"{AllWhitespace}A{AllWhitespace}C{AllWhitespace}");
            Assert.Equal("A C", content);
        }

        [Fact]
        public void WhitespaceOtherThanSpaceIsConvertedToSpace()
        {
            var content = TestContentControlContent("A B\rC\nD\tE\r\nF");
            Assert.Equal("A B C D E F", content);
        }

        [Fact]
        public void CommentNodesAreIgnoredForWhitespaceNormalization()
        {
            var content = TestContentControlContent(" <!-- X --> ");
            Assert.Null(content);
        }

        // This is per XML specification https://www.w3.org/TR/2006/REC-xml11-20060816/#sec-line-ends
        [Fact]
        [Trait("Category", "xml:space='preserve'")]
        public void CarriageReturnIsTranslatedToNewLineEvenWithXmlSpacePreserve()
        {
            var content = TestContentControlContent("A\rB\r\nC", xmlPreserve: true);
            Assert.Equal("A\nB\nC", content);
        }

        // This is non-obvious, but remaining whitespace nodes are still stripped, even when using
        // xml:space="preserve", unless the control opts-in to whitespace significance.
        // In case a text-node is not purely whitespace, i.e. " A ", the leading and trailing whitespace
        // is preserved due to xml:space="preserve", and it is not dropped even for controls not opting into
        // whitespace.
        [Fact]
        [Trait("Category", "xml:space='preserve'")]
        public void WhiteSpaceOnlyTextNodesAreStrippedForControlsNotOptingInEvenWithXmlSpacePreserve()
        {
            var content = TestContentControlContent(AllWhitespace, xmlPreserve: true);
            Assert.Null(content);
        }

        [Fact]
        [Trait("Category", "xml:space='preserve'")]
        public void LeadingAndTrailingWhiteSpaceIsNotTrimmedWithXmlSpacePreserve()
        {
            var content = TestContentControlContent($"{AllWhitespace}X{AllWhitespace}", xmlPreserve: true);
            Assert.Equal($"{AllWhitespace}X{AllWhitespace}", content);
        }

        [Fact]
        [Trait("Category", "xml:space='preserve'")]
        public void InnerWhitespaceIsPreservedAndNotCollapsedWithXmlSpacePreserve()
        {
            var content =
                TestContentControlContent($"{AllWhitespace}A{AllWhitespace}C{AllWhitespace}", xmlPreserve: true);
            Assert.Equal($"{AllWhitespace}A{AllWhitespace}C{AllWhitespace}", content);
        }

        [Fact]
        [Trait("Category", "xml:space='preserve'")]
        public void XmlSpacePreserveIsInheritedFromParent()
        {
            var content = TestContentControlContent<ContentControl>(
                $"<ContentControl>{AllWhitespace}A{AllWhitespace}</ContentControl>", xmlPreserve: true);
            Assert.Equal($"{AllWhitespace}A{AllWhitespace}", content.Content);
        }

        [Fact]
        [Trait("Category", "PropertySetters")]
        public void WhiteSpaceBeforeBetweenAndAfterPropertySettersIsTrimmed()
        {
            var content =
                TestContentControlContent(
                    $"{AllWhitespace}<Control.Tag>Red</Control.Tag>{AllWhitespace}<Control.Focusable>false</Control.Focusable> CONTENT");
            Assert.Equal("CONTENT", content);
        }

        [Fact]
        [Trait("Category", "PropertySetters")]
        public void WhiteSpaceBeforeAndBetweenAndPropertySettersIsTrimmedWithXmlSpacePreserve()
        {
            var content =
                TestContentControlContent(
                    $"{AllWhitespace}<Control.StrProp>Red</Control.StrProp>{AllWhitespace}<Control.BoolProp>false</Control.BoolProp> CONTENT",
                    xmlPreserve: true);
            Assert.Equal(" CONTENT", content);
        }

        [Fact]
        [Trait("Category", "PropertySetters")]
        public void XmlSpacePreserveDoesNotAffectAttributeValueNormalization()
        {
            var xaml = $"<ContentControl Content=\"{AllWhitespace}X{AllWhitespace}\" />";
            var content = TestContentControlContent<ContentControl>(xaml);
            // This normalization is due to XML spec 3.3.3 Attribute-Value Normalization
            Assert.Equal("   X   ", content.Content);

            content = TestContentControlContent<ContentControl>(xaml, xmlPreserve:true);
            // This normalization is due to XML spec 3.3.3 Attribute-Value Normalization
            Assert.Equal("   X   ", content.Content);
        }

        // See XML spec 3.3.3 Attribute-Value Normalization
        [Fact]
        [Trait("Category", "PropertySetters")]
        public void CharacterEntitiesInAttributesAreNotSubjectToAttributeValueNormalization()
        {
            var xaml = $"<ContentControl Content=\" &#xA;&#x9;\" />";
            var content = TestContentControlContent<ContentControl>(xaml);
            Assert.Equal(AllWhitespace, content.Content);
        }

        [Fact]
        [Trait("Category", "PropertySetters")]
        public void XmlSpacePreserveAffectsPropertySetterElement()
        {
            var content = TestContentControlContent($"<ContentControl.Content>{AllWhitespace}</ContentControl.Content>");
            Assert.Null(content);

            content = TestContentControlContent($"<ContentControl.Content>{AllWhitespace}</ContentControl.Content>", xmlPreserve:true);
            Assert.Equal(AllWhitespace, content);
        }

        [Fact]
        [Trait("Category", "MixedContent")]
        public void WhiteSpaceAroundNestedControlIsTrimmedWithoutOptIn()
        {
            var content = TestMixedContent($"{AllWhitespace}<Control/>{AllWhitespace}");
            Assert.Collection(content, c => Assert.IsType<Control>(c));
        }

        [Fact]
        [Trait("Category", "MixedContent")]
        public void WhiteSpaceAroundNestedControlIsTrimmedWithoutOptInEvenWithXmlSpacePreserve()
        {
            var content = TestMixedContent($"{AllWhitespace}<Control/>{AllWhitespace}", preserveSpace: true);
            Assert.Collection(content, c => Assert.IsType<Control>(c));
        }

        [Fact]
        [Trait("Category", "MixedContent")]
        public void TextAcrossCommentsIsMergedInto()
        {
            var content = TestMixedContent($"{AllWhitespace}<!-- X -->{AllWhitespace}", preserveSpace: true,
                whiteSpaceOptIn: true);
            Assert.Collection(content, c => Assert.Equal(AllWhitespace + AllWhitespace, c));
        }

        // Due to the following normalization rules from the XAML documentation:
        // - A space immediately following the start tag is deleted.
        // - A space immediately before the end tag is deleted.
        [Fact]
        [Trait("Category", "MixedContent")]
        public void WhiteSpaceAtStartAndEndIsRemovedEvenWithWhitespaceOptIn()
        {
            var content = TestMixedContent($"{AllWhitespace}<Control/>{AllWhitespace}", whiteSpaceOptIn: true);
            Assert.Collection(content,
                c => Assert.IsType<Control>(c)
            );
        }

        [Fact]
        [Trait("Category", "MixedContent")]
        public void WhiteSpaceAtStartAndEndIsPreservedWithBothOptInAndXmlSpacePreserve()
        {
            var content = TestMixedContent($"{AllWhitespace}<Control />{AllWhitespace}", whiteSpaceOptIn: true,
                preserveSpace: true);
            Assert.Collection(content,
                c => Assert.Equal(AllWhitespace, c),
                c => Assert.IsType<Control>(c),
                c => Assert.Equal(AllWhitespace, c)
            );
        }

        [Fact]
        [Trait("Category", "TrimSurroundingWhitespace")]
        public void WhiteSpaceInNodesAroundNormalControlIsPreservedWhenOptingIn()
        {
            var content = TestMixedContent("A <Control/> B", whiteSpaceOptIn:true);
            Assert.Collection(
                content,
                x => Assert.Equal("A ", x),
                x => Assert.IsType<Control>(x),
                x => Assert.Equal(" B", x)
            );
        }

        [Fact]
        [Trait("Category", "TrimSurroundingWhitespace")]
        public void WhiteSpaceInNodesAroundTrimAroundControlIsTrimmedWhenOptingIn()
        {
            var content = TestMixedContent("A <TrimControl/> B", whiteSpaceOptIn:true);
            Assert.Collection(
                content,
                x => Assert.Equal("A", x),
                x => Assert.IsType<TrimControl>(x),
                x => Assert.Equal("B", x)
            );
        }

        private object TestContentControlContent(string rawContent, bool xmlPreserve = false)
        {
            var xmlSpaceAttr = xmlPreserve ? " xml:space='preserve'" : "";
            var control =
                ReadXaml<ContentControl>($"<ContentControl {Ns}{xmlSpaceAttr}>{rawContent}</ContentControl>");
            return control.Content;
        }

        private T TestContentControlContent<T>(string rawContent, bool xmlPreserve = false)
        {
            return Assert.IsType<T>(TestContentControlContent(rawContent, xmlPreserve));
        }

        private IEnumerable<object> TestMixedContent(string rawContent, bool preserveSpace = false,
            bool whiteSpaceOptIn = false)
        {
            var controlName = whiteSpaceOptIn ? "WhitespaceOptInControl" : "MixedContentControl";
            var xmlSpaceAttr = preserveSpace ? " xml:space='preserve'" : "";
            var xaml = $"<{controlName} {Ns}{xmlSpaceAttr}>{rawContent}</{controlName}>";
            _output.WriteLine(xaml);
            var control = ReadXaml<object>(xaml);
            if (whiteSpaceOptIn)
            {
                return ((WhitespaceOptInControl) control).Content;
            }

            return ((MixedContentControl) control).Content;
        }

        private T ReadXaml<T>(string xaml) where T : class
        {
            var result = CompileAndRun(xaml);
            if (!(result is T))
            {
                throw new Exception($"Wanted: {typeof(T)}, got: {result}");
            }

            return (T) result;
        }

    }

    public class Control
    {
        public string StrProp { get; set; }

        public bool BoolProp { get; set; }
    }

    public class ContentControl : Control
    {
        [Content]
        public object Content { get; set; }
    }

    public class MixedContentControl
    {
        [Content]
        public List<object> Content { get; } = new List<object>();
    }

    public class WhitespaceOptInControl
    {
        [Content]
        public WhitespaceOptInCollection Content { get; } = new WhitespaceOptInCollection();
    }

    // TODO [WhitespaceSignificantCollection]
    public class WhitespaceOptInCollection : List<object>
    {
    }

    // TODO [TrimSurroundingWhitespace]
    public class TrimControl
    {
    }
}
