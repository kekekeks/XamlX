using System;

namespace XamlParserTests
{
    public class ServiceProviderValueExtension
    {
        public object ProvideValue(IServiceProvider sp) =>
            ((ExtensionValueHolder)sp.GetService(typeof(ExtensionValueHolder))).Value;
    }
}
