using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace XamlX.TypeSystem
{
    public partial class CecilTypeSystem
    {
        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        class CecilType : IXamlType, ITypeReference
        {
            private readonly CecilAssembly _assembly;
            public CecilTypeSystem TypeSystem { get; }
            public TypeReference Reference { get; }
            public TypeDefinition Definition { get; }

            public CecilType(CecilTypeSystem typeSystem, CecilAssembly assembly, TypeDefinition definition)
                : this(typeSystem, assembly, definition, definition)
            {
                
            }
            
            public CecilType(CecilTypeSystem typeSystem, CecilAssembly assembly, TypeDefinition definition,
                TypeReference reference)
            {
                _assembly = assembly;
                TypeSystem = typeSystem;
                Reference = reference;
                Definition = definition;
            }

            public bool Equals(IXamlType other)
            {
                if (ReferenceEquals(this, other))
                    return true;
                if (!(other is CecilType o))
                    return false;
                return Equals(Reference, o.Reference);
            }

            public object Id => Reference.FullName;
            public string Name => Reference.Name;
            public string FullName => Reference.FullName;
            public string Namespace => Reference.Namespace;
            public IXamlAssembly Assembly => _assembly;
            protected IReadOnlyList<IXamlMethod> _methods;

            public IReadOnlyList<IXamlMethod> Methods =>
                _methods ?? (_methods = Definition.GetMethods().Select(m => new CecilMethod(TypeSystem,
                    m, Reference)).ToList());

            protected IReadOnlyList<IXamlConstructor> _constructors;

            public IReadOnlyList<IXamlConstructor> Constructors =>
                _constructors ?? (_constructors = Definition.GetConstructors()
                    .Select(c => new CecilConstructor(TypeSystem, c, Reference)).ToList());

            protected IReadOnlyList<IXamlField> _fields;

            public IReadOnlyList<IXamlField> Fields =>
                _fields ?? (_fields = Definition.Fields
                    .Select(f => new CecilField(TypeSystem, f, Reference)).ToList());

            protected IReadOnlyList<IXamlProperty> _properties;

            public IReadOnlyList<IXamlProperty> Properties =>
                _properties ?? (_properties =
                    Definition.Properties.Select(p => new CecilProperty(TypeSystem, p, Reference)).ToList());

            private IReadOnlyList<IXamlType> _genericArguments;

            public IReadOnlyList<IXamlType> GenericArguments =>
                _genericArguments ?? (_genericArguments = Reference is GenericInstanceType gi
                    ? gi.GenericArguments.Select(ga => TypeSystem.Resolve(ga)).ToList()
                    : null);
            
            protected IReadOnlyList<IXamlCustomAttribute> _attributes;
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ?? (_attributes =
                    Definition.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem, ca)).ToList());

            public bool IsAssignableFrom(IXamlType type)
            {
                if (!type.IsValueType
                    && type == XamlPseudoType.Null)
                    return true;
                if (type.IsValueType && type.GenericTypeDefinition?.FullName == "System.Nullable`1")
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
                    return TypeSystem.GetTypeFor(i);
                }
                throw new InvalidOperationException();
            }

            private IXamlType _genericTypeDefinition;

            public IXamlType GenericTypeDefinition =>
                _genericTypeDefinition ?? (_genericTypeDefinition =
                    (Reference is GenericInstanceType) ? TypeSystem.Resolve(Definition) : null);

            private IXamlType _baseType;

            public IXamlType BaseType => Definition.BaseType == null
                ? null
                : _baseType ?? (_baseType = TypeSystem.Resolve(
                      Definition.BaseType.TransformGeneric(Reference)));
            
            public bool IsValueType => Definition.IsValueType;
            public bool IsEnum => Definition.IsEnum;
            protected IReadOnlyList<IXamlType> _interfaces;

            public IReadOnlyList<IXamlType> Interfaces =>
                _interfaces ?? (_interfaces =
                    Definition.Interfaces.Select(i => TypeSystem.Resolve(i.InterfaceType
                        .TransformGeneric(Reference))).ToList());
            
            public bool IsInterface => Definition.IsInterface;
            public IXamlType GetEnumUnderlyingType()
            {
                if (!IsEnum)
                    return null;
                return TypeSystem.Resolve(Definition.GetEnumUnderlyingType());
            }
            
            
        }
    }
}