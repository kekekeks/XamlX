using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilProperty : IXamlProperty
        {
            private readonly CecilTypeResolveContext _typeResolveContext;
            public PropertyDefinition Property { get; }

            public CecilProperty(CecilTypeResolveContext typeResolveContext, PropertyDefinition property)
            {
                _typeResolveContext = typeResolveContext;
                Property = property;
            }

            public string Name => Property.Name;

            private IXamlType? _type;

            public IXamlType PropertyType =>
                _type ??= _typeResolveContext.ResolvePropertyType(Property);

            private IXamlMethod? _setter;

            public IXamlMethod? Setter => Property.SetMethod == null
                ? null
                : _setter ??= new CecilMethod(_typeResolveContext, Property.SetMethod);
            
            private IXamlMethod? _getter;

            public IXamlMethod? Getter => Property.GetMethod == null
                ? null
                : _getter ??= new CecilMethod(_typeResolveContext, Property.GetMethod);

            private IReadOnlyList<IXamlCustomAttribute>? _attributes;

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ??= Property.CustomAttributes.Select(ca => new CecilCustomAttribute(_typeResolveContext, ca)).ToList();

            private IReadOnlyList<IXamlType>? _indexerParameters;

            public IReadOnlyList<IXamlType> IndexerParameters =>
                _indexerParameters ??= Property.Parameters.Select(param => _typeResolveContext.ResolveParameterType(Property, param)).ToList();

            private IXamlType? _declaringType;

            public IXamlType DeclaringType
                => _declaringType ??= _typeResolveContext.Resolve(Property.DeclaringType);

            public bool Equals(IXamlProperty? other) =>
                other is CecilProperty cf
                && TypeReferenceEqualityComparer.AreEqual(Property.DeclaringType, cf.Property.DeclaringType, CecilTypeComparisonMode.Exact)
                && cf.Property.FullName == Property.FullName;

            public override bool Equals(object? other) => Equals(other as IXamlProperty);

            public override int GetHashCode() =>
                (TypeReferenceEqualityComparer.GetHashCodeFor(Property.DeclaringType, CecilTypeComparisonMode.Exact), Property.FullName).GetHashCode();

            public override string ToString() => Property.ToString();
        }
    }
}
