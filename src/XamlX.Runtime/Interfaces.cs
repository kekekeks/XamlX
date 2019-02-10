using System;
using System.Collections.Generic;

namespace XamlX.Runtime
{
    public interface IXamlXParentStackProviderV1
    {
        IEnumerable<object> Parents { get; }
    }

    public class XamlXXmlNamespaceInfoV1
    {
        public string ClrNamespace { get; set; }
        public string ClrAssemblyName { get; set; }
    }
    
    public interface IXamlXXmlNamespaceInfoProviderV1
    {
        IReadOnlyDictionary<string, IReadOnlyList<XamlXXmlNamespaceInfoV1>> XmlNamespaces { get; }
    }
}