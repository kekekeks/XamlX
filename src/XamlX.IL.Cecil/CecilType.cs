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
        // TODO: Make generic type definitions have Reference set to GenericTypeInstance with parameters for
        // consistency with CecilTypeBuilder
        
        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        class CecilType : IXamlType, ITypeReference
        {
            private readonly CecilAssembly _assembly;
            public CecilTypeSystem TypeSystem { get; }
            public TypeReference Reference { get; }
            public TypeDefinition Definition { get; }

            private Dictionary<IXamlType, bool> _isAssignableFromCache = new Dictionary<IXamlType, bool>();
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
                if (reference.IsArray)
                    Definition = ((CecilType)typeSystem.GetType("System.Array")).Definition;
            }

            public bool Equals(IXamlType other)
            {
                if (ReferenceEquals(this, other))
                    return true;
                if (!(other is CecilType o))
                    return false;
                return CecilHelpers.Equals(Reference, o.Reference);
            }

            public override string ToString() => Definition.ToString();

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
            
            protected IReadOnlyList<IXamlEventInfo> _events;

            public IReadOnlyList<IXamlEventInfo> Events =>
                _events ?? (_events =
                    Definition.Events.Select(p => new CecilEvent(TypeSystem, p, Reference)).ToList());

            private IReadOnlyList<IXamlType> _genericArguments;

            public IReadOnlyList<IXamlType> GenericArguments =>
                _genericArguments ?? (_genericArguments = Reference is GenericInstanceType gi
                    ? gi.GenericArguments.Select(ga => TypeSystem.Resolve(ga)).ToList()
                    : null);

            private IReadOnlyList<IXamlType> _genericParameters;

            public IReadOnlyList<IXamlType> GenericParameters =>
                _genericParameters ?? (_genericParameters = Reference is TypeDefinition td && td.HasGenericParameters
                    ? td.GenericParameters.Select(gp => TypeSystem.Resolve(gp)).ToList()
                    : null);
            

            protected IReadOnlyList<IXamlCustomAttribute> _attributes;
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ?? (_attributes =
                    Definition.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem, ca)).ToList());

            public bool IsAssignableFrom(IXamlType type)
            {
                if (_isAssignableFromCache.TryGetValue(type, out var cached))
                    return cached;
                return _isAssignableFromCache[type] = IsAssignableFromCore(type);
            }
            bool IsAssignableFromCore(IXamlType type)
            {
                if (!type.IsValueType
                    && type == XamlPseudoType.Null)
                    return true;
                
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
                    return TypeSystem.GetTypeFor(i);
                }
                throw new InvalidOperationException();
            }

            private IXamlType _genericTypeDefinition;

            public IXamlType GenericTypeDefinition =>
                _genericTypeDefinition ?? (_genericTypeDefinition =
                    (Reference is GenericInstanceType) ? TypeSystem.Resolve(Definition) : null);

            public bool IsArray => Reference.IsArray;

            private IXamlType _arrayType;

            public IXamlType ArrayElementType =>
                _arrayType ?? (_arrayType =
                    IsArray ? TypeSystem.Resolve(Definition) : null);

            public IXamlType MakeArrayType(int dimensions) => TypeSystem.Resolve(Reference.MakeArrayType(dimensions));

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
