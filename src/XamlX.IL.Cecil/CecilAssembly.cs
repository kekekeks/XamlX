using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        internal class CecilAssembly : IXamlAssembly
        {
            private readonly Dictionary<string, IXamlType> _typeCache = new();

            public CecilTypeSystem TypeSystem { get; }
            public AssemblyDefinition Assembly { get; }

            public CecilAssembly(CecilTypeSystem typeSystem, AssemblyDefinition assembly)
            {
                TypeSystem = typeSystem;
                Assembly = assembly;
            }

            public bool Equals(IXamlAssembly? other) => other == this;

            public string Name => Assembly.Name.Name;
            private IReadOnlyList<IXamlCustomAttribute>? _attributes;

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ??= Assembly.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem.RootTypeResolveContext, ca)).ToList();

            public IXamlType? FindType(string fullName)
            {
                if (_typeCache.TryGetValue(fullName, out var rv))
                    return rv;
                var asmRef = new AssemblyNameReference(Assembly.Name.Name, Assembly.Name.Version);
                var lastDot = fullName.LastIndexOf('.');
                var ns = string.Empty;

                if (lastDot != -1)
                {
                    ns = fullName.Substring(0, lastDot);
                    fullName = fullName.Substring(lastDot + 1);
                }

                TypeReference? tref = null;
                TypeDefinition? tdef = null;
                var plus = fullName.IndexOf('+');

                while (true)
                {
                    var typeName = plus != -1 ? fullName.Substring(0, plus) : fullName;
                    var t = new TypeReference(ns, typeName, Assembly.MainModule, asmRef);

                    t.DeclaringType = tref;
                    tref = t;
                    tdef = tref?.Resolve();
                    if (tdef is null || tdef.IsNestedPrivate)
                    {
                        return null;
                    }
                    if (plus == -1)
                        break;

                    ns = null;
                    fullName = fullName.Substring(plus + 1);
                    plus = fullName.IndexOf('+');
                }
                return _typeCache[fullName] = TypeSystem.RootTypeResolveContext.Resolve(tdef);
            }
        }
    }
}
