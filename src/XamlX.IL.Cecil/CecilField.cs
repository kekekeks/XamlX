using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilField : IXamlField
        {
            private readonly FieldDefinition _def;
            private readonly CecilTypeResolveContext _typeResolveContext;
            public FieldReference Field { get; }

            public CecilField(CecilTypeResolveContext typeResolveContext, FieldDefinition def)
            {
                _typeResolveContext = typeResolveContext;
                _def = def;

                // Can't use FieldDefinition for IL, because it doesn't hold generics information.
                Field = typeResolveContext.ResolveReference(def);
            }

            public string Name => Field.Name;
            private IXamlType? _type;
            public IXamlType FieldType => _type ??= _typeResolveContext.ResolveFieldType(Field);
            public bool IsPublic => _def.IsPublic;
            public bool IsStatic => _def.IsStatic;
            public bool IsLiteral => _def.IsLiteral;

            private IReadOnlyList<IXamlCustomAttribute>? _attributes;

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ??= _def.CustomAttributes.Select(ca => new CecilCustomAttribute(_typeResolveContext, ca)).ToList();

            private IXamlType? _declaringType;

            public IXamlType DeclaringType
                => _declaringType ??= _typeResolveContext.Resolve(Field.DeclaringType);

            public object GetLiteralValue()
            {
                if (IsLiteral && _def.HasConstant)
                    return _def.Constant;
                throw new InvalidOperationException($"{this} isn't a literal");
            }

            public bool Equals(IXamlField? other) =>
                other is CecilField cf
                && TypeReferenceEqualityComparer.AreEqual(Field.DeclaringType, cf.Field.DeclaringType, CecilTypeComparisonMode.Exact)
                && cf.Field.FullName == Field.FullName;

            public override bool Equals(object? other) => Equals(other as IXamlField);

            public override int GetHashCode() =>
                (TypeReferenceEqualityComparer.GetHashCodeFor(Field.DeclaringType, CecilTypeComparisonMode.Exact), Field.FullName).GetHashCode();

            public override string ToString() => Field.ToString();
        }
    }
}
