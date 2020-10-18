using System;

namespace XamlParserTests
{
    public delegate object CallbackExtensionCallback(IServiceProvider provider);

    public class CallbackExtension
    {
        public object ProvideValue(IServiceProvider provider)
        {
            return provider.GetService<CallbackExtensionCallback>()(provider);
        }
    }
}
