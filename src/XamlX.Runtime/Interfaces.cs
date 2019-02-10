using System;
using System.Collections.Generic;

namespace XamlX.Runtime
{
    public interface IXamlXParentStackProviderV1
    {
        IEnumerable<object> Parents { get; }
    }
}