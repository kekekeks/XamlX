using System.Collections.Generic;
using XamlX.Runtime;

namespace XamlParserTests
{
    public class ListParentsProvider : List<object>, IXamlParentStackProviderV1
    {
        public IEnumerable<object> Parents => this;
    }
}
