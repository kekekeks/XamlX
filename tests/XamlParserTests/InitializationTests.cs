using System.Collections.Generic;
using Xunit;

namespace XamlParserTests
{
    public class InitializationTests : CompilerTestBase
    {
        public static string BeginInitEvent = EventConstants.BeginInitEvent;
        public static string EndInitEvent = EventConstants.BeginInitEvent;
        public static string PropertySetEvent = EventConstants.PropertySetEvent;
        public static string ChildAddedEvent = EventConstants.ChildAddedEvent;

        public InitializationTests()
        {
            InitializationTestsClass.Reset();
        }
        
        [Fact]
        public void Initialization_Events_Should_Be_Triggered_For_Supports_Initialize()
        {
            
            CompileAndRun(@"
<InitializationTestsSupportInitializeClass xmlns='test' Property='123'>
    <InitializationTestsSupportInitializeClass.Child>
        <InitializationTestsSupportInitializeClass Property='321'/>
    </InitializationTestsSupportInitializeClass.Child>
    <InitializationTestsSupportInitializeClass Property='321'/>
</InitializationTestsSupportInitializeClass>");
            Helpers.StructDiff(new List<string>
            {
                "1:BeginInit",
                "1:PropertySet",
                "2:BeginInit",
                "2:PropertySet",
                "2:EndInit",
                "1:ChildAdded:2",
                "3:BeginInit",
                "3:PropertySet",
                "3:EndInit",
                "1:ChildAdded:3",
                "1:EndInit"
            }, InitializationTestsClass.Events);
        }
        
        [Fact]
        public void UsableDuringInitialization_Should_Revert_Initialization_Order()
        {
            CompileAndRun(@"
<InitializationTestsTopDownClass xmlns='test' Property='123'>
    <InitializationTestsTopDownClass.Child>
        <InitializationTestsTopDownClass Property='321'/>
    </InitializationTestsTopDownClass.Child>
    <InitializationTestsTopDownClass Property='321'/>
</InitializationTestsTopDownClass>");
            Helpers.StructDiff(new List<string>
            {
                "1:BeginInit",
                "1:PropertySet",
                "2:BeginInit",
                "1:ChildAdded:2",
                "2:PropertySet",
                "2:EndInit",
                "3:BeginInit",
                "1:ChildAdded:3",
                "3:PropertySet",
                "3:EndInit",
                "1:EndInit"
            }, InitializationTestsClass.Events);
        }
    }
}