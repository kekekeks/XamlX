using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace XamlParserTests.Impl
{
    [Collection("GuiLabs")] public class GuiLabsBasicCompilerTests : BasicCompilerTests {}
    [Collection("GuiLabs")] public class GuiLabsConvertersTests : ConvertersTests { }
    [Collection("GuiLabs")] public class GuiLabsDeferredContentTests : DeferredContentTests { }
    [Collection("GuiLabs")] public class GuiLabsDictionaryTests : DictionaryTests { }
    [Collection("GuiLabs")] public class GuiLabsInitializationTests : InitializationTests { }
    [Collection("GuiLabs")] public class GuiLabsIntrinsicsTests : IntrinsicsTests { }
    [Collection("GuiLabs")] public class GuiLabsListTests : ListTests { }
    [Collection("GuiLabs")] public class GuiLabsMarkupExtensionTests : MarkupExtensionTests { }
    [Collection("GuiLabs")] public class GuiLabsServiceProviderTests : ServiceProviderTests { }
    [Collection("GuiLabs")] public class GuiLabsParserTests : ParserTests { }
}
