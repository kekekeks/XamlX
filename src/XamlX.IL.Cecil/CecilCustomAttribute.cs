using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {       
        class CecilCustomAttribute : IXamlCustomAttribute
        {
            
            public CecilTypeSystem TypeSystem { get; }
            public CustomAttribute Data { get; }

            public CecilCustomAttribute(CecilTypeSystem typeSystem, CustomAttribute data)
            {
                TypeSystem = typeSystem;
                Data = data;
            }

            public bool Equals(IXamlCustomAttribute other) => other is CecilCustomAttribute ca && ca.Data == Data;

            private IXamlType _type;
            public IXamlType Type => _type ?? (_type = TypeSystem.Resolve(Data.AttributeType));

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

            public Dictionary<string, object> Properties
            {
                get
                {
                    if (_properties is null)
                    {
                        Dictionary<string, object> properties = new Dictionary<string, object>();
                        foreach (var prop in Data.Properties)
                        {
                            properties.Add(prop.Name, ConvertValue(prop.Argument.Value));
                        }
                        foreach (var field in Data.Fields)
                        {
                            properties.Add(field.Name, ConvertValue(field.Argument.Value));
                        }
                        _properties = properties;
                    }
                    return _properties;
                }
            }
        }
    }
}