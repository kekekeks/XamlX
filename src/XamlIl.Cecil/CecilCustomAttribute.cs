using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlIl.TypeSystem
{
    partial class CecilTypeSystem
    {       
        class CecilCustomAttribute : IXamlIlCustomAttribute
        {
            
            public CecilTypeSystem TypeSystem { get; }
            public CustomAttribute Data { get; }

            public CecilCustomAttribute(CecilTypeSystem typeSystem, CustomAttribute data)
            {
                TypeSystem = typeSystem;
                Data = data;
            }

            public bool Equals(IXamlIlCustomAttribute other) => other is CecilCustomAttribute ca && ca.Data == Data;

            private IXamlIlType _type;
            public IXamlIlType Type => _type ?? (_type = TypeSystem.Resolve(Data.AttributeType));

            private List<object> _parameters;

            object ConvertValue(object value)
            {
                if (value is TypeReference tr)
                    return TypeSystem.GetTypeFor(tr);
                return value;
            }

            public List<object> Parameters =>
                _parameters ?? (_parameters = Data.ConstructorArguments.Select(d => ConvertValue(d.Value)).ToList());

            private Dictionary<string, object> _properties;

            public Dictionary<string, object> Properties =>
                _properties ?? (_properties =
                    Data.Properties.ToDictionary(d => d.Name, d => ConvertValue(d.Argument.Value)));
        }
    }
}