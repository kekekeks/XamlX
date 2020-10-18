using System;

namespace XamlParserTests
{
    public class UnknownServiceUsageExtension
    {
        public static event Action<IServiceProvider> ProvideValueEventRequiredAssert;

        public object Return { get; set; }

        public object ProvideValue(IServiceProvider provider)
        {
            ProvideValueEventRequiredAssert.Invoke(provider);
            return Return;
        }
    }
}
