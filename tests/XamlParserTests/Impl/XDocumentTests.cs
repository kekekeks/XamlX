using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace XamlParserTests.Impl
{
    [Collection("XDocument")] public class XDocumentBasicCompilerTests : BasicCompilerTests {}
    [Collection("XDocument")] public class XDocumentConvertersTests : ConvertersTests { }
    [Collection("XDocument")] public class XDocumentDeferredContentTests : DeferredContentTests { }
    [Collection("XDocument")] public class XDocumentDictionaryTests : DictionaryTests { }
    [Collection("XDocument")] public class XDocumentInitializationTests : InitializationTests { }
    [Collection("XDocument")] public class XDocumentIntrinsicsTests : IntrinsicsTests { }
    [Collection("XDocument")] public class XDocumentListTests : ListTests { }
    [Collection("XDocument")] public class XDocumentMarkupExtensionTests : MarkupExtensionTests { }
    [Collection("XDocument")] public class XDocumentServiceProviderTests : ServiceProviderTests { }
    [Collection("XDocument")] public class XDocumentParserTests : ParserTests { }
}
