using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlIl.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilProperty : IXamlIlProperty
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

            public bool Equals(IXamlIlProperty other) => other is CecilProperty cp && cp.Property == Property;

            public string Name => Property.Name;
            private IXamlIlType _type;

            public IXamlIlType PropertyType =>
                _type ?? (_type = TypeSystem.Resolve(Property.PropertyType.TransformGeneric(_declaringType)));
            private IXamlIlMethod _setter;

            public IXamlIlMethod Setter => Property.SetMethod == null
                ? null
                : _setter ?? (_setter = TypeSystem.Resolve(Property.SetMethod, _declaringType));
            
            private IXamlIlMethod _getter;

            public IXamlIlMethod Getter => Property.GetMethod == null
                ? null
                : _getter ?? (_getter = TypeSystem.Resolve(Property.GetMethod, _declaringType));

            private IReadOnlyList<IXamlIlCustomAttribute> _attributes;
            public IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes =>
                _attributes ?? (_attributes =
                    Property.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem, ca)).ToList());

            private IReadOnlyList<IXamlIlType> _indexerParameters;
            public IReadOnlyList<IXamlIlType> IndexerParameters =>
                _indexerParameters ?? (_indexerParameters =
                    Property.Parameters.Select(param => TypeSystem.Resolve(param.ParameterType.TransformGeneric(_declaringType))).ToList());
        }
    }
}
