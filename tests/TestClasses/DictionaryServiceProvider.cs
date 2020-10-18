using System;
using System.Collections.Generic;

namespace XamlParserTests
{
    public class DictionaryServiceProvider : Dictionary<Type, object>, IServiceProvider
    {
        public IServiceProvider Parent { get; set; }

        public object GetService(Type serviceType)
        {
            if (TryGetValue(serviceType, out var impl))
                return impl;
            return Parent?.GetService(serviceType);
        }
    }
}
