using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilProperty : IXamlProperty
        {
            private readonly TypeReference _declaringType;
            public CecilTypeSystem TypeSystem { get; }
            public PropertyDefinition Property { get; }

            public CecilProperty(CecilTypeSystem typeSystem, PropertyDefinition property, TypeReference declaringType)
            {
                _declaringType = declaringType;
                TypeSystem = typeSystem;
                Property = property;
            }

            public bool Equals(IXamlProperty other) => other is CecilProperty cp && cp.Property == Property;

            public string Name => Property.Name;
            private IXamlType _type;

            public IXamlType PropertyType =>
                _type ?? (_type = TypeSystem.Resolve(Property.PropertyType.TransformGeneric(_declaringType)));
            private IXamlMethod _setter;

            public IXamlMethod Setter => Property.SetMethod == null
                ? null
                : _setter ?? (_setter = TypeSystem.Resolve(Property.SetMethod, _declaringType));
            
            private IXamlMethod _getter;

            public IXamlMethod Getter => Property.GetMethod == null
                ? null
                : _getter ?? (_getter = TypeSystem.Resolve(Property.GetMethod, _declaringType));

            private IReadOnlyList<IXamlCustomAttribute> _attributes;
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ?? (_attributes =
                    Property.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem, ca)).ToList());

            private IReadOnlyList<IXamlType> _indexerParameters;
            public IReadOnlyList<IXamlType> IndexerParameters =>
                _indexerParameters ?? (_indexerParameters =
                    Property.Parameters.Select(param => TypeSystem.Resolve(param.ParameterType.TransformGeneric(_declaringType))).ToList());
        }
    }
}
