using System;
using Xunit;

namespace XamlParserTests
{
    public class UnknownServiceUsageExtension
    {
        public object Return { get; set; }

        public object ProvideValue(IServiceProvider provider)
        {
            Assert.Null(provider.GetService(typeof(string)));
            return Return;
        }
    }
}
