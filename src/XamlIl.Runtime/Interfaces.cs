using System;
using System.Collections.Generic;

namespace XamlIl.Runtime
{
    public interface IXamlIlParentStackProviderV1
    {
        IEnumerable<object> Parents { get; }
    }

    public class XamlIlXmlNamespaceInfoV1
    {
        public string ClrNamespace { get; set; }
        public string ClrAssemblyName { get; set; }
    }
    
    public interface IXamlIlXmlNamespaceInfoProviderV1
    {
        IReadOnlyDictionary<string, IReadOnlyList<XamlIlXmlNamespaceInfoV1>> XmlNamespaces { get; }
    }
}