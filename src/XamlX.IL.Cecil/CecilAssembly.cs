using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        private class CecilAssembly : IXamlAssembly
        {
            private Dictionary<string, CecilType> _typeCache = new Dictionary<string, CecilType>();
            public CecilTypeSystem TypeSystem { get; }
            public AssemblyDefinition Assembly { get; }

            public CecilAssembly(CecilTypeSystem typeSystem, AssemblyDefinition assembly)
            {
                TypeSystem = typeSystem;
                Assembly = assembly;
            }

            public bool Equals(IXamlAssembly other) => other == this;

            public string Name => Assembly.Name.Name;
            private IReadOnlyList<IXamlCustomAttribute> _attributes;

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ?? (_attributes =
                    Assembly.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem, ca)).ToList());
            
            public IXamlType FindType(string fullName)
            {
                if (_typeCache.TryGetValue(fullName, out var rv))
                    return rv;
                var lastDot = fullName.LastIndexOf(".", StringComparison.Ordinal);
                var asmRef = new AssemblyNameReference(Assembly.Name.Name, Assembly.Name.Version);
                var tref = (lastDot == -1)
                    ? new TypeReference(null, fullName, Assembly.MainModule, asmRef)
                    : new TypeReference(fullName.Substring(0, lastDot),
                        fullName.Substring(lastDot + 1), Assembly.MainModule, asmRef);
                var resolved = tref.Resolve();
                if (resolved != null)
                    return _typeCache[fullName] = TypeSystem.GetTypeFor(resolved);

                return null;

            }
        }
    }
}
