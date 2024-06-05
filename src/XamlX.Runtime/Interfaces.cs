using System.Collections.Generic;

namespace XamlX.Runtime
{
    public interface IXamlParentStackProviderV1
    {
        IEnumerable<object> Parents { get; }
    }

    public class XamlXmlNamespaceInfoV1
    {
        public string ClrNamespace { get; set; } = null!;
        public string ClrAssemblyName { get; set; } = null!;
    }
    
    public interface IXamlXmlNamespaceInfoProviderV1
    {
        IReadOnlyDictionary<string, IReadOnlyList<XamlXmlNamespaceInfoV1>> XmlNamespaces { get; }
    }
}