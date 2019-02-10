using System;
using System.Collections.Generic;

namespace XamlIl.Runtime
{
    public interface IXamlIlParentStackProviderV1
    {
        IEnumerable<object> Parents { get; }
    }
}