using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        // As per the XAML docs, these three characters are considered whitespace,
        // we also add \r to simulate character entities bypassing the XML parser
        private const string AllWhitespace = " \n\t";

        // CharacterEntity representation of AllWhitespace
        private const string CharacterEntities = "&#x20;&#xA;&#x9;";

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
        public void CharacterEntitiesAreRecognizedAsWhitespace()
        {
            // We add a carriage return here since it can be used to bypass the parsers normalization
            // and does behave differently between \r and &#xD; (\r always gets converted to \n, while &#xD; does not).
            var entities = $"{CharacterEntities}&#xD;";
            var content = TestContentControlContent($"{entities}A{entities}B{entities}");
            Assert.Equal("A B", content);
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

        // As an exception to the aforementioned spec, character entities bypass the XML parser's normalization
        // and will result in \r being passed through when xml:space=preserve is being used.
        [Fact]
        [Trait("Category", "xml:space='preserve'")]
        public void CarriageReturnCharacterEntityIsMaintainedWithXmlSpacePreserve()
        {
            var content = TestContentControlContent("A&#x0D;B&#x0D;\nC", xmlPreserve: true);
            Assert.Equal("A\rB\r\nC", content);
        }

        // This behavior differs from WPF, where a string property maintains the white-space, while an
        // object property does not.
        [Fact]
        [Trait("Category", "xml:space='preserve'")]
        public void WhiteSpaceOnlyTextNodesAreStrippedForControlsNotOptingInEvenWithXmlSpacePreserve()
        {
            var content = TestContentControlContent(AllWhitespace, xmlPreserve: true);
            Assert.Equal(AllWhitespace, content);
        }

        [Fact]
        [Trait("Category", "xml:space='preserve'")]
        public void StringPropertiesWillReceiveWhitespaceOnlyWithXmlSpacePreserve()
        {
            var content = TestContentControlContent<Control>($"<Control><Control.StrProp>{AllWhitespace}</Control.StrProp></Control>", xmlPreserve: true);
            Assert.Equal(AllWhitespace, content.StrProp);
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
                    $"{AllWhitespace}<Control.StrProp>Red</Control.StrProp>{AllWhitespace}<Control.BoolProp>false</Control.BoolProp> CONTENT");
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

        [Theory]
        [Trait("Category", "PropertySetters")]
        [InlineData(false)]
        [InlineData(true)]
        public void XmlSpacePreserveDoesNotAffectAttributeValueNormalization(bool xmlPreserve)
        {
            var xaml = $"<ContentControl Content=\"{AllWhitespace}X{AllWhitespace}\" />";
            var content = TestContentControlContent<ContentControl>(xaml, xmlPreserve);
            // This normalization is due to XML spec 3.3.3 Attribute-Value Normalization
            Assert.Equal("   X   ", content.Content);
        }

        // See XML spec 3.3.3 Attribute-Value Normalization
        [Fact]
        [Trait("Category", "PropertySetters")]
        public void CharacterEntitiesInAttributesAreNotSubjectToAttributeValueNormalization()
        {
            var xaml = $"<ContentControl Content=\"{CharacterEntities}\" />";
            var content = TestContentControlContent<ContentControl>(xaml);
            Assert.Equal(AllWhitespace, content.Content);
        }

        [Fact]
        [Trait("Category", "PropertySetters")]
        public void XmlSpaceDefaultAppliesPropertySetterElementNormalization()
        {
            // Whitespace normalization is applied normally
            var content =
                TestContentControlContent($"<ContentControl.Content>{AllWhitespace}</ContentControl.Content>");
            Assert.Null(content);
        }

        // xml:space=preserve can be used to disable whitespace normalization for property setters too,
        // even though the schema does not allow the attribute to be set on the property-setter itself,
        // it's value is inherited from the parent.
        [Fact]
        [Trait("Category", "PropertySetters")]
        public void XmlSpacePreserveDoesNotApplyPropertySetterElementNormalization()
        {
            // Whitespace normalization isn't applied, because the parent has xml:space="preserve"
            var content = TestContentControlContent($"<ContentControl.Content>{AllWhitespace}</ContentControl.Content>",
                xmlPreserve: true);
            Assert.Equal(AllWhitespace, content);
        }

        [Fact]
        [Trait("Category", "MixedContent")]
        public void WhiteSpaceAroundNestedControlIsTrimmedWithoutOptIn()
        {
            var content = TestMixedContent($"{AllWhitespace}<Control/>{AllWhitespace}<Control/>{AllWhitespace}");
            Assert.Collection(content,
                c => Assert.IsType<Control>(c),
                c => Assert.IsType<Control>(c)
            );
        }

        [Fact]
        [Trait("Category", "MixedContent")]
        public void WhiteSpaceAroundNestedControlIsTrimmedWithoutOptInEvenWithXmlSpacePreserve()
        {
            var content = TestMixedContent($"{AllWhitespace}<Control/>{AllWhitespace}<Control/>{AllWhitespace}",
                preserveSpace: true);
            Assert.Collection(content,
                c => Assert.IsType<Control>(c),
                c => Assert.IsType<Control>(c)
            );
        }

        [Fact]
        [Trait("Category", "MixedContent")]
        public void TextAcrossCommentsIsMerged()
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

        // This is important for TextBlock for space between Spans/Runs and normal text
        [Fact]
        [Trait("Category", "TrimSurroundingWhitespace")]
        public void WhiteSpaceInNodesAroundNormalControlIsPreservedWhenOptingIn()
        {
            var content = TestMixedContent("A <Control/> B", whiteSpaceOptIn: true);
            Assert.Collection(
                content,
                x => Assert.Equal("A ", x),
                x => Assert.IsType<Control>(x),
                x => Assert.Equal(" B", x)
            );
        }

        // This is important for TextBlock for space between Spans/Runs.
        [Fact]
        [Trait("Category", "TrimSurroundingWhitespace")]
        public void WhitespaceBetweenNonTextNodesIsPreservedWhenOptingIn()
        {
            var content = TestMixedContent("<Control/> <Control/>", whiteSpaceOptIn: true);
            Assert.Collection(
                content,
                x => Assert.IsType<Control>(x),
                x => Assert.Equal(" ", x),
                x => Assert.IsType<Control>(x)
            );
        }

        [Fact]
        [Trait("Category", "TrimSurroundingWhitespace")]
        public void WhitespaceInTextNodesAroundTrimAroundControlIsTrimmedWhenOptingIn()
        {
            var content = TestMixedContent("A <TrimControl/> B", whiteSpaceOptIn: true);
            Assert.Collection(
                content,
                x => Assert.Equal("A", x),
                x => Assert.IsType<TrimControl>(x),
                x => Assert.Equal("B", x)
            );
        }

        [Fact]
        [Trait("Category", "TrimSurroundingWhitespace")]
        public void TrimSurroundingWhitespaceIsDisabledByXmlSpacePreserve()
        {
            var content = TestMixedContent("A <TrimControl/> B", whiteSpaceOptIn: true, preserveSpace: true);
            Assert.Collection(
                content,
                x => Assert.Equal("A ", x),
                x => Assert.IsType<TrimControl>(x),
                x => Assert.Equal(" B", x)
            );
        }

        [Fact]
        [Trait("Category", "TrimSurroundingWhitespace")]
        public void WhitespaceSurroundingTrimControlIsTrimmedWhenOptingIn()
        {
            // The comments in the string ensure that this happens AFTER merging text nodes OR the algorithm
            // is capable of trimming multiple whitespace nodes.
            var content = TestMixedContent("<Control/> <!-- --> <TrimControl/> <!-- --> <Control/>",
                whiteSpaceOptIn: true);
            Assert.Collection(
                content,
                x => Assert.IsType<Control>(x),
                x => Assert.IsType<TrimControl>(x),
                x => Assert.IsType<Control>(x)
            );
        }
        
        [Fact]
        [Trait("Category", "MixedContent")]
        public void WhitespaceShouldBeTrimmedForPlainIEnumerableContentProperty()
        {
            var content = ReadXaml<MixedEnumerableContentControl>($"<MixedEnumerableContentControl xmlns='test'>{AllWhitespace}<Control/>{AllWhitespace}<Control/>{AllWhitespace}</MixedEnumerableContentControl>");
            Assert.Collection(content.Items.Cast<object>(),
                c => Assert.IsType<Control>(c),
                c => Assert.IsType<Control>(c));
        }

        [Fact]
        [Trait("Category", "TrimSurroundingWhitespace")]
        public void WhitespaceShouldNotBeTrimmedForWhitespaceOptInCollection()
        {
            var xaml = @"
<ControlWithInlines  xmlns='test'>
   with <InlineWithInlines>several</InlineWithInlines>
    <InlineWithInlines>Span</InlineWithInlines>
  </ControlWithInlines>
";       
                var content = ReadXaml<ControlWithInlines>(xaml);

                Assert.Collection(
                   content.Inlines,
                   x =>
                   {
                       Assert.IsType<Run>(x);
                       var run = (Run)x;
                       Assert.Equal("with ", run.Text);
                   },
                   x =>
                   {
                       Assert.IsType<InlineWithInlines>(x);

                       var span = (InlineWithInlines)x;

                       Assert.Single(span.Inlines);

                       var inline = span.Inlines[0];

                       Assert.IsType<Run>(inline);

                       var run = (Run)inline;

                       Assert.Equal("several", run.Text);
                   },
                   x => {
                       Assert.IsType<Run>(x);
                       var run = (Run)x;
                       Assert.Equal(" ", run.Text);
                   },
                   x =>
                   {
                       Assert.IsType<InlineWithInlines>(x);

                       var span = (InlineWithInlines)x;

                       Assert.Single(span.Inlines);

                       var inline = span.Inlines[0];

                       Assert.IsType<Run>(inline);

                       var run = (Run)inline;

                       Assert.Equal("Span", run.Text);
                   }
               );
            
        }

        private object? TestContentControlContent(string rawContent, bool xmlPreserve = false)
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
            if (result is not T)
            {
                throw new Exception($"Wanted: {typeof(T)}, got: {result}");
            }

            return (T) result;
        }
    }

    public class Control
    {
        public string? StrProp { get; set; }

        public bool BoolProp { get; set; }
    }

    public class ContentControl : Control
    {
        [Content]
        public object? Content { get; set; }
    }

    public class MixedContentControl
    {
        [Content]
        public List<object> Content { get; } = new List<object>();
    }

    public class MixedEnumerableContentControl
    {
        [Content] public IEnumerable Items { get; set; } = new List<object>();
    }

    // This control uses a collection as it's content property which declares
    // the WhitespaceSignificantCollection property.
    public class WhitespaceOptInControl
    {
        [Content]
        public WhitespaceOptInCollection Content { get; } = new ();
    }

    [WhitespaceSignificantCollection]
    public class WhitespaceOptInCollection : List<object>
    {
    }

    public class ControlWithInlines
    {
        [Content]
        public InlineCollection Inlines { get; set; } = new(); //The setter here is important because it causes whitespace removal
    }

    public class Inline
    {

    }

    public class Run : Inline
    {
        public string? Text { get; set; }
    }

    public class InlineWithInlines : Inline
    {
        [Content]
        public InlineCollection Inlines { get; set; } = new(); //The setter here is important because it causes whitespace removal
    }

    [WhitespaceSignificantCollection]
    public class InlineCollection : List<Inline>
    {
        public void Add(string text)
        {
            Add(new Run { Text = text });
        }
    }

    [TrimSurroundingWhitespace]
    public class TrimControl
    {
    }
}