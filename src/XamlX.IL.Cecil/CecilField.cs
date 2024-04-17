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
            private readonly CecilTypeResolver _typeResolver;
            public FieldReference Field { get; }

            public CecilField(CecilTypeResolver typeResolver, FieldDefinition def)
            {
                _typeResolver = typeResolver;
                _def = def;

                // Can't use FieldDefinition for IL, because it doesn't hold generics information.
                Field = typeResolver.ResolveReference(def);
            }

            public string Name => Field.Name;
            private IXamlType _type;
            public IXamlType FieldType => _type ??= _typeResolver.ResolveFieldType(Field);
            public bool IsPublic => _def.IsPublic;
            public bool IsStatic => _def.IsStatic;
            public bool IsLiteral => _def.IsLiteral;

            private IReadOnlyList<IXamlCustomAttribute> _attributes;
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ??= _def.CustomAttributes.Select(ca => new CecilCustomAttribute(_typeResolver, ca)).ToList();

            public object GetLiteralValue()
            {
                if (IsLiteral && _def.HasConstant)
                    return _def.Constant;
                return null;
            }

            public bool Equals(IXamlField other) =>
                other is CecilField cf
                && TypeReferenceEqualityComparer.AreEqual(Field.DeclaringType, cf.Field.DeclaringType)
                && cf.Field.FullName == Field.FullName;

            public override bool Equals(object other) => Equals(other as IXamlField); 

            public override int GetHashCode() =>
                (TypeReferenceEqualityComparer.GetHashCodeFor(Field.DeclaringType), Field.FullName).GetHashCode();

            public override string ToString() => Field.ToString();
        }
    }
}
