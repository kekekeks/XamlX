using System;
using System.Collections.Generic;

namespace XamlX.Runtime
{
    public interface IXamlParentStackProviderV1
    {
        IEnumerable<object> Parents { get; }
    }

    public class XamlXmlNamespaceInfoV1
    {
        public string ClrNamespace { get; set; }
        public string ClrAssemblyName { get; set; }
    }
    
    public interface IXamlXmlNamespaceInfoProviderV1
    {
        IReadOnlyDictionary<string, IReadOnlyList<XamlXmlNamespaceInfoV1>> XmlNamespaces { get; }
    }
}