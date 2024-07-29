using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        internal class CecilType : IXamlType, ITypeReference
        {
            private readonly CecilAssembly? _assembly;
            public CecilTypeSystem TypeSystem { get; }
            public TypeReference Reference { get; }
            public TypeDefinition Definition { get; }

            private readonly Dictionary<IXamlType, bool> _isAssignableFromCache = new();

            public CecilType(CecilTypeResolveContext parentTypeResolveContext, CecilAssembly? assembly, TypeDefinition definition)
                : this(parentTypeResolveContext, assembly, definition, definition)
            {
                
            }
            
            public CecilType(CecilTypeResolveContext parentTypeResolveContext, CecilAssembly? assembly, TypeDefinition definition,
                TypeReference reference)
            {
                _assembly = assembly;
                TypeSystem = parentTypeResolveContext.TypeSystem;
                Reference = reference;
                Definition = definition;
                if (reference.IsArray)
                    Definition = ((CecilType)TypeSystem.GetType("System.Array")).Definition;

                TypeResolveContext = parentTypeResolveContext.Nested(reference);
            }

            public object Id => Reference.FullName;
            public string Name => Reference.Name;
            public string FullName => Reference.FullName;
            public string Namespace => Reference.Namespace;
            public bool IsPublic => Definition.IsPublic;
            public bool IsNestedPrivate => Definition.IsNestedPrivate;
            protected CecilTypeResolveContext TypeResolveContext { get; }

            public IXamlAssembly? Assembly => _assembly;
            protected IReadOnlyList<IXamlMethod>? _methods;

            public IReadOnlyList<IXamlMethod> Methods =>
                _methods ??= Definition.GetMethods().Select(m => new CecilMethod(TypeResolveContext, m)).ToList();

            protected IReadOnlyList<IXamlConstructor>? _constructors;

            public IReadOnlyList<IXamlConstructor> Constructors =>
                _constructors ??= Definition.GetConstructors()
                    .Select(c => new CecilConstructor(TypeResolveContext, c)).ToList();

            protected IReadOnlyList<IXamlField>? _fields;

            public IReadOnlyList<IXamlField> Fields =>
                _fields ??= Definition.Fields
                    .Select(f => new CecilField(TypeResolveContext, f)).ToList();

            protected IReadOnlyList<IXamlProperty>? _properties;

            public IReadOnlyList<IXamlProperty> Properties =>
                _properties ??= Definition.Properties.Select(p => new CecilProperty(TypeResolveContext, p)).ToList();
            
            protected IReadOnlyList<IXamlEventInfo>? _events;

            public IReadOnlyList<IXamlEventInfo> Events =>
                _events ??= Definition.Events.Select(p => new CecilEvent(TypeResolveContext, p)).ToList();

            private IReadOnlyList<IXamlType>? _genericArguments;

            public IReadOnlyList<IXamlType> GenericArguments =>
                _genericArguments ??= Reference is GenericInstanceType gi
                    ? gi.GenericArguments.Select(ga => TypeResolveContext.Resolve(ga)).ToList()
                    : Array.Empty<IXamlType>();

            private IReadOnlyList<IXamlType>? _genericParameters;

            public IReadOnlyList<IXamlType> GenericParameters =>
                _genericParameters ??= Reference is TypeDefinition { HasGenericParameters: true } td
                    ? td.GenericParameters.Select(gp => TypeResolveContext.Resolve(gp)).ToList()
                    : Array.Empty<IXamlType>();

            protected IReadOnlyList<IXamlCustomAttribute>? _attributes;

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ??= Definition.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeResolveContext, ca)).ToList();

            public bool IsAssignableFrom(IXamlType type)
            {
                if (_isAssignableFromCache.TryGetValue(type, out var cached))
                    return cached;
                return _isAssignableFromCache[type] = IsAssignableFromCore(type);
            }
            bool IsAssignableFromCore(IXamlType type)
            {
                if (type == XamlPseudoType.Null)
                    return !IsValueType || GenericTypeDefinition?.FullName == "System.Nullable`1";

                if (type.IsValueType 
                    && GenericTypeDefinition?.FullName == "System.Nullable`1"
                    && GenericArguments[0].Equals(type))
                    return true;
                if (FullName == "System.Object" && type.IsInterface)
                    return true;
                var baseType = type;
                while (baseType != null)
                {
                    if (baseType.Equals(this))
                        return true;
                    baseType = baseType.BaseType;
                }

                if (IsInterface && type.GetAllInterfaces().Any(IsAssignableFrom))
                    return true;
                return false;
            }

            public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments)
            {
                if (Reference == Definition)
                {
                    var i = Definition.MakeGenericInstanceType(typeArguments.Cast<ITypeReference>().Select(r => r.Reference)
                        .ToArray());
                    return TypeResolveContext.Resolve(i);
                }
                throw new InvalidOperationException();
            }

            private IXamlType? _genericTypeDefinition;

            public IXamlType? GenericTypeDefinition =>
                _genericTypeDefinition ??= (Reference is GenericInstanceType) ? TypeResolveContext.Resolve(Definition, false) : null;

            public bool IsArray => Reference.IsArray;

            private IXamlType? _arrayType;

            public IXamlType? ArrayElementType =>
                _arrayType ??= IsArray ? TypeResolveContext.Resolve(Reference.GetElementType()) : null;

            public IXamlType MakeArrayType(int dimensions) => TypeResolveContext.Resolve(Reference.MakeArrayType(dimensions));

            private IXamlType? _baseType;

            public IXamlType? BaseType => Definition.BaseType == null
                ? null
                : _baseType ??= TypeResolveContext.Resolve(Definition.BaseType);

            private IXamlType? _declaringType;

            public IXamlType? DeclaringType =>
                Definition.DeclaringType == null
                    ? null
                    : _declaringType ??= TypeResolveContext.Resolve(Definition.DeclaringType);

            public bool IsValueType => Definition.IsValueType;
            public bool IsEnum => Definition.IsEnum;
            protected IReadOnlyList<IXamlType>? _interfaces;

            public IReadOnlyList<IXamlType> Interfaces =>
                _interfaces ??= Definition.Interfaces.Select(i => TypeResolveContext.Resolve(i.InterfaceType)).ToList();

            public bool IsInterface => Definition.IsInterface;
            public IXamlType GetEnumUnderlyingType()
            {
                if (!IsEnum)
                    throw new InvalidOperationException($"{this} isn't an enum");
                return TypeResolveContext.Resolve(Definition.GetEnumUnderlyingType());
            }

            // Note: should always be false, as a TypeDefinition can't be a function pointer.
            public bool IsFunctionPointer => Definition.IsFunctionPointer;

            public bool Equals(IXamlType? other)
            {
                if (!(other is CecilType o))
                    return false;
                return TypeReferenceEqualityComparer.AreEqual(Reference, o.Reference, CecilTypeComparisonMode.Exact);
            }

            public override bool Equals(object? other) => Equals(other as IXamlType);

            public override int GetHashCode() => TypeReferenceEqualityComparer.GetHashCodeFor(Reference, CecilTypeComparisonMode.Exact);

            public override string ToString() => Definition.ToString();
        }
    }
}
