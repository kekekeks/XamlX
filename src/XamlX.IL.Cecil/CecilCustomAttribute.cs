using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {       
        class CecilCustomAttribute : IXamlCustomAttribute
        {
            private readonly CecilTypeResolveContext _typeResolveContext;

            public CustomAttribute Data { get; }

            public CecilCustomAttribute(CecilTypeResolveContext typeResolveContext, CustomAttribute data)
            {
                _typeResolveContext = typeResolveContext;
                Data = data;
            }

            public bool Equals(IXamlCustomAttribute? other) => other is CecilCustomAttribute ca && ca.Data == Data;

            private IXamlType? _type;
            public IXamlType Type => _type ??= _typeResolveContext.Resolve(Data.AttributeType);

            private List<object?>? _parameters;

            object? ConvertValue(object? value)
            {
                if (value is TypeReference tr)
                    return _typeResolveContext.Resolve(tr);
                if (value is CustomAttributeArgument attr)
                    return attr.Value;
                if (value is IEnumerable<CustomAttributeArgument> array)
                    return array.Select(a => ConvertValue(a)).ToArray();
                return value;
            }

            public List<object?> Parameters =>
                _parameters ??= Data.ConstructorArguments.Select(d => ConvertValue(d.Value)).ToList();

            private Dictionary<string, object?>? _properties;

            public Dictionary<string, object?> Properties
            {
                get
                {
                    if (_properties is null)
                    {
                        var properties = new Dictionary<string, object?>();
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
