using System;

namespace XamlParserTests
{
    public class InnerProvider : IServiceProvider, ITestRootObjectProvider
    {
        public static IServiceProvider InnerProviderFactory(IServiceProvider outer) =>
            new InnerProvider(outer);

        private ITestRootObjectProvider _originalRootObjectProvider;

        public object RootObject => "Definitely not the root object";

        public object OriginalRootObject => _originalRootObjectProvider.RootObject;

        public InnerProvider(IServiceProvider parent)
        {
            _originalRootObjectProvider = parent.GetService<ITestRootObjectProvider>();
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(ITestRootObjectProvider))
                return this;
            return null;
        }
    }
}
