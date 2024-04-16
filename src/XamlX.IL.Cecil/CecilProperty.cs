using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilProperty : IXamlProperty
        {
            private readonly CecilTypeResolver _typeResolver;
            public PropertyDefinition Property { get; }

            public CecilProperty(CecilTypeResolver typeResolver, PropertyDefinition property)
            {
                _typeResolver = typeResolver;
                Property = property;
            }

            public string Name => Property.Name;
            private IXamlType _type;

            public IXamlType PropertyType =>
                _type ??= _typeResolver.ResolvePropertyType(Property);
            private IXamlMethod _setter;

            public IXamlMethod Setter => Property.SetMethod == null
                ? null
                : _setter ??= new CecilMethod(_typeResolver, Property.SetMethod);
            
            private IXamlMethod _getter;

            public IXamlMethod Getter => Property.GetMethod == null
                ? null
                : _getter ??= new CecilMethod(_typeResolver, Property.GetMethod);

            private IReadOnlyList<IXamlCustomAttribute> _attributes;
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ??= Property.CustomAttributes.Select(ca => new CecilCustomAttribute(_typeResolver, ca)).ToList();

            private IReadOnlyList<IXamlType> _indexerParameters;
            public IReadOnlyList<IXamlType> IndexerParameters =>
                _indexerParameters ??= Property.Parameters.Select(param => _typeResolver.ResolveParameterType(Property, param)).ToList();

            public bool Equals(IXamlProperty other) =>
                other is CecilProperty cf
                && TypeReferenceEqualityComparer.AreEqual(Property.DeclaringType, cf.Property.DeclaringType)
                && cf.Property.FullName == Property.FullName;

            public override bool Equals(object other) => Equals(other as IXamlProperty); 

            public override int GetHashCode() =>
                (TypeReferenceEqualityComparer.GetHashCodeFor(Property.DeclaringType), Property.FullName).GetHashCode();

            public override string ToString() => Property.ToString();
        }
    }
}
