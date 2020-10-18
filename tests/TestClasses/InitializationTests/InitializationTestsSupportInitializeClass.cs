using System.ComponentModel;

namespace XamlParserTests
{
    public class InitializationTestsSupportInitializeClass : InitializationTestsClass, ISupportInitialize
    {
        public void BeginInit()
        {
            AddEvent(EventConstants.BeginInitEvent);
        }

        public void EndInit()
        {
            AddEvent(EventConstants.EndInitEvent);
        }
    }
}
