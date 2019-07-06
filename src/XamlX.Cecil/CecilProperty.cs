using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilProperty : IXamlXProperty
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

            public bool Equals(IXamlXProperty other) => other is CecilProperty cp && cp.Property == Property;

            public string Name => Property.Name;
            private IXamlXType _type;

            public IXamlXType PropertyType =>
                _type ?? (_type = TypeSystem.Resolve(Property.PropertyType.TransformGeneric(_declaringType)));
            private IXamlXMethod _setter;

            public IXamlXMethod Setter => Property.SetMethod == null
                ? null
                : _setter ?? (_setter = TypeSystem.Resolve(Property.SetMethod, _declaringType));
            
            private IXamlXMethod _getter;

            public IXamlXMethod Getter => Property.GetMethod == null
                ? null
                : _getter ?? (_getter = TypeSystem.Resolve(Property.GetMethod, _declaringType));

            private IReadOnlyList<IXamlXCustomAttribute> _attributes;
            public IReadOnlyList<IXamlXCustomAttribute> CustomAttributes =>
                _attributes ?? (_attributes =
                    Property.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem, ca)).ToList());

            private IReadOnlyList<IXamlXType> _indexerParameters;
            public IReadOnlyList<IXamlXType> IndexerParameters =>
                _indexerParameters ?? (_indexerParameters =
                    Property.Parameters.Select(param => TypeSystem.Resolve(param.ParameterType.TransformGeneric(_declaringType))).ToList());
        }
    }
}
