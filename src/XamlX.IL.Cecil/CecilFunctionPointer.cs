using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace XamlX.TypeSystem;

partial class CecilTypeSystem
{
    internal class CecilFunctionPointerType : IXamlType, ITypeReference
    {
        private readonly FunctionPointerType _reference;

        public CecilFunctionPointerType(FunctionPointerType reference) => _reference = reference;

        public TypeReference Reference => _reference;
        public object Id => _reference.FullName;
        public string Name => _reference.Name;
        public string FullName => _reference.FullName;
        public string Namespace => _reference.Namespace;
        public bool IsPublic => true;
        public bool IsNestedPrivate => false;
        public IXamlAssembly? Assembly => null;
        public IReadOnlyList<IXamlProperty> Properties => Array.Empty<IXamlProperty>();
        public IReadOnlyList<IXamlEventInfo> Events => Array.Empty<IXamlEventInfo>();
        public IReadOnlyList<IXamlField> Fields => Array.Empty<IXamlField>();
        public IReadOnlyList<IXamlMethod> Methods => Array.Empty<IXamlMethod>();
        public IReadOnlyList<IXamlConstructor> Constructors => Array.Empty<IXamlConstructor>();
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => Array.Empty<IXamlCustomAttribute>();
        public IReadOnlyList<IXamlType> GenericArguments => Array.Empty<IXamlType>();
        public IReadOnlyList<IXamlType> Interfaces => Array.Empty<IXamlType>();
        public IReadOnlyList<IXamlType> GenericParameters => Array.Empty<IXamlType>();
        public bool IsArray => false;
        public bool IsValueType => false;
        public bool IsEnum => false;
        public bool IsInterface => false;
        public bool IsFunctionPointer => true;
        public IXamlType? GenericTypeDefinition => null;
        public IXamlType? ArrayElementType => null;
        public IXamlType? BaseType => null;
        public IXamlType? DeclaringType => null;

        public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments) => throw new InvalidOperationException();
        public IXamlType MakeArrayType(int dimensions) => throw new InvalidOperationException();
        public IXamlType GetEnumUnderlyingType() => throw new InvalidOperationException();

        public bool Equals(IXamlType? other)
            => other is CecilFunctionPointerType typedOther &&
               TypeReferenceEqualityComparer.AreEqual(Reference, typedOther.Reference, CecilTypeComparisonMode.Exact);

        public override bool Equals(object? obj) => Equals(obj as IXamlType);
        public override int GetHashCode() => TypeReferenceEqualityComparer.GetHashCodeFor(Reference, CecilTypeComparisonMode.Exact);
        public bool IsAssignableFrom(IXamlType type) => Equals(type);
    }
}