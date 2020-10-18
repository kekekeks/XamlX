using System;

namespace XamlParserTests
{
    public class ServiceProviderIntValueExtension
    {
        public int ProvideValue(IServiceProvider sp) =>
            (int)((ExtensionValueHolder)sp.GetService(typeof(ExtensionValueHolder))).Value;
    }
}
