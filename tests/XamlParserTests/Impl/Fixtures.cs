using System;
using System.Collections.Generic;
using System.Text;
using XamlX.Parsers;
using Xunit;

namespace XamlParserTests.Impl
{

    public class GuiLabsFixture : IDisposable
    {
        public GuiLabsFixture()
        {
            XamlParser.UseLegacyParser = false;
        }

        public void Dispose()
        {
        }
    }

    public class XDocumentFixture : IDisposable
    {
        public XDocumentFixture()
        {
            XamlParser.UseLegacyParser = true;
        }

        public void Dispose()
        {
        }
    }

    [CollectionDefinition("GuiLabs")]
    public class GuiLabsCollection : ICollectionFixture<XDocumentFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
    [CollectionDefinition("XDocument")]
    public class XDocumentCollection : ICollectionFixture<XDocumentFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
