using System.Collections.Generic;

namespace XamlX.Parsers
{
#if !XAMLX_INTERNAL
    public
#endif
    class XmlNamespaces
    {
        private readonly Dictionary<string, string> _prefixToNamespace;
        private readonly HashSet<string> _ignoredNamespaces;
        private readonly Dictionary<string, string> _compatibilityMappings;

        public XmlNamespaces(Dictionary<string, string> prefixToNamespace, HashSet<string> ignoredNamespaces, Dictionary<string, string> compatibilityMappings)
        {
            _prefixToNamespace = prefixToNamespace;
            _ignoredNamespaces = ignoredNamespaces;
            _compatibilityMappings = compatibilityMappings;
        }

        public static (string prefix, string name) GetPrefixFromName(string name)
        {
            var colonIndex = name.LastIndexOf(":");
            if(colonIndex == -1)
            {
                return ("", name);
            }
            else
            {
                return (name.Substring(0, colonIndex), name.Substring(colonIndex+1));
            }
        }

        public string? DefaultNamespace
        {
            get
            {
                _prefixToNamespace.TryGetValue("", out string? ns);
                return ns;
            }
        }

        public (string? ns, string prefix, string name) GetNsFromName(string name)
        {
            (string prefix, string localName) = GetPrefixFromName(name);
            _prefixToNamespace.TryGetValue(prefix, out string? ns);
            if (ns != null && _compatibilityMappings.TryGetValue(ns, out string? mappedNs))
            {
                ns = mappedNs;
            }
            return (ns, prefix, localName);
        }

        internal bool IsIgnorable(string? attrNs)
        {
            return attrNs is not null && _ignoredNamespaces.Contains(attrNs);
        }

        internal string? NsForPrefix(string prefix)
        {
            if(_prefixToNamespace.TryGetValue(prefix, out string? ns))
            {
                if (_compatibilityMappings.TryGetValue(ns, out string? mappedNs))
                {
                    ns = mappedNs;
                }
            }
            
            return ns;
        }
    }
}
