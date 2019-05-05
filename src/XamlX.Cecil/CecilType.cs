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
        class CecilType : IXamlXType, ITypeReference
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
                if (reference.IsArray)
                    Definition = ((CecilType)typeSystem.GetType("System.Array")).Definition;
            }

            public bool Equals(IXamlXType other)
            {
                if (ReferenceEquals(this, other))
                    return true;
                if (!(other is CecilType o))
                    return false;
                return CecilHelpers.Equals(Reference, o.Reference);
            }

            public object Id => Reference.FullName;
            public string Name => Reference.Name;
            public string FullName => Reference.FullName;
            public string Namespace => Reference.Namespace;
            public IXamlXAssembly Assembly => _assembly;
            protected IReadOnlyList<IXamlXMethod> _methods;

            public IReadOnlyList<IXamlXMethod> Methods =>
                _methods ?? (_methods = Definition.GetMethods().Select(m => new CecilMethod(TypeSystem,
                    m, Reference)).ToList());

            protected IReadOnlyList<IXamlXConstructor> _constructors;

            public IReadOnlyList<IXamlXConstructor> Constructors =>
                _constructors ?? (_constructors = Definition.GetConstructors()
                    .Select(c => new CecilConstructor(TypeSystem, c, Reference)).ToList());

            protected IReadOnlyList<IXamlXField> _fields;

            public IReadOnlyList<IXamlXField> Fields =>
                _fields ?? (_fields = Definition.Fields
                    .Select(f => new CecilField(TypeSystem, f, Reference)).ToList());

            protected IReadOnlyList<IXamlXProperty> _properties;

            public IReadOnlyList<IXamlXProperty> Properties =>
                _properties ?? (_properties =
                    Definition.Properties.Select(p => new CecilProperty(TypeSystem, p, Reference)).ToList());
            
            protected IReadOnlyList<IXamlXEventInfo> _events;

            public IReadOnlyList<IXamlXEventInfo> Events =>
                _events ?? (_events =
                    Definition.Events.Select(p => new CecilEvent(TypeSystem, p, Reference)).ToList());

            private IReadOnlyList<IXamlXType> _genericArguments;

            public IReadOnlyList<IXamlXType> GenericArguments =>
                _genericArguments ?? (_genericArguments = Reference is GenericInstanceType gi
                    ? gi.GenericArguments.Select(ga => TypeSystem.Resolve(ga)).ToList()
                    : null);

            private IReadOnlyList<IXamlXType> _genericParameters;

            public IReadOnlyList<IXamlXType> GenericParameters =>
                _genericParameters ?? (_genericParameters = Reference is TypeDefinition td && td.HasGenericParameters
                    ? td.GenericParameters.Select(gp => TypeSystem.Resolve(gp)).ToList()
                    : null);
            

            protected IReadOnlyList<IXamlXCustomAttribute> _attributes;
            public IReadOnlyList<IXamlXCustomAttribute> CustomAttributes =>
                _attributes ?? (_attributes =
                    Definition.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem, ca)).ToList());

            public bool IsAssignableFrom(IXamlXType type)
            {
                if (!type.IsValueType
                    && type == XamlXPseudoType.Null)
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

            public IXamlXType MakeGenericType(IReadOnlyList<IXamlXType> typeArguments)
            {
                if (Reference == Definition)
                {
                    var i = Definition.MakeGenericInstanceType(typeArguments.Cast<ITypeReference>().Select(r => r.Reference)
                        .ToArray());
                    return TypeSystem.GetTypeFor(i);
                }
                throw new InvalidOperationException();
            }

            private IXamlXType _genericTypeDefinition;

            public IXamlXType GenericTypeDefinition =>
                _genericTypeDefinition ?? (_genericTypeDefinition =
                    (Reference is GenericInstanceType) ? TypeSystem.Resolve(Definition) : null);

            public bool IsArray => Reference.IsArray;

            private IXamlXType _arrayType;

            public IXamlXType ArrayElementType =>
                _arrayType ?? (_arrayType =
                    IsArray ? TypeSystem.Resolve(Definition) : null);

            public IXamlXType MakeArrayType(int dimensions) => TypeSystem.Resolve(Reference.MakeArrayType(dimensions));

            private IXamlXType _baseType;

            public IXamlXType BaseType => Definition.BaseType == null
                ? null
                : _baseType ?? (_baseType = TypeSystem.Resolve(
                      Definition.BaseType.TransformGeneric(Reference)));
            
            public bool IsValueType => Definition.IsValueType;
            public bool IsEnum => Definition.IsEnum;
            protected IReadOnlyList<IXamlXType> _interfaces;

            public IReadOnlyList<IXamlXType> Interfaces =>
                _interfaces ?? (_interfaces =
                    Definition.Interfaces.Select(i => TypeSystem.Resolve(i.InterfaceType
                        .TransformGeneric(Reference))).ToList());
            
            public bool IsInterface => Definition.IsInterface;
            public IXamlXType GetEnumUnderlyingType()
            {
                if (!IsEnum)
                    return null;
                return TypeSystem.Resolve(Definition.GetEnumUnderlyingType());
            }
            
            
        }
    }
}
