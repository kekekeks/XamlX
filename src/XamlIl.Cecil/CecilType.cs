using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace XamlIl.TypeSystem
{
    partial class CecilTypeSystem
    {
        // TODO: Make generic type definitions have Reference set to GenericTypeInstance with parameters for
        // consistency with CecilTypeBuilder
        
        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        class CecilType : IXamlIlType, ITypeReference
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

            public bool Equals(IXamlIlType other)
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
            public IXamlIlAssembly Assembly => _assembly;
            protected IReadOnlyList<IXamlIlMethod> _methods;

            public IReadOnlyList<IXamlIlMethod> Methods =>
                _methods ?? (_methods = Definition.GetMethods().Select(m => new CecilMethod(TypeSystem,
                    m, Reference)).ToList());

            protected IReadOnlyList<IXamlIlConstructor> _constructors;

            public IReadOnlyList<IXamlIlConstructor> Constructors =>
                _constructors ?? (_constructors = Definition.GetConstructors()
                    .Select(c => new CecilConstructor(TypeSystem, c, Reference)).ToList());

            protected IReadOnlyList<IXamlIlField> _fields;

            public IReadOnlyList<IXamlIlField> Fields =>
                _fields ?? (_fields = Definition.Fields
                    .Select(f => new CecilField(TypeSystem, f, Reference)).ToList());

            protected IReadOnlyList<IXamlIlProperty> _properties;

            public IReadOnlyList<IXamlIlProperty> Properties =>
                _properties ?? (_properties =
                    Definition.Properties.Select(p => new CecilProperty(TypeSystem, p, Reference)).ToList());
            
            protected IReadOnlyList<IXamlIlEventInfo> _events;

            public IReadOnlyList<IXamlIlEventInfo> Events =>
                _events ?? (_events =
                    Definition.Events.Select(p => new CecilEvent(TypeSystem, p, Reference)).ToList());

            private IReadOnlyList<IXamlIlType> _genericArguments;

            public IReadOnlyList<IXamlIlType> GenericArguments =>
                _genericArguments ?? (_genericArguments = Reference is GenericInstanceType gi
                    ? gi.GenericArguments.Select(ga => TypeSystem.Resolve(ga)).ToList()
                    : null);

            private IReadOnlyList<IXamlIlType> _genericParameters;

            public IReadOnlyList<IXamlIlType> GenericParameters =>
                _genericParameters ?? (_genericParameters = Reference is TypeDefinition td && td.HasGenericParameters
                    ? td.GenericParameters.Select(gp => TypeSystem.Resolve(gp)).ToList()
                    : null);
            

            protected IReadOnlyList<IXamlIlCustomAttribute> _attributes;
            public IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes =>
                _attributes ?? (_attributes =
                    Definition.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem, ca)).ToList());

            public bool IsAssignableFrom(IXamlIlType type)
            {
                if (!type.IsValueType
                    && type == XamlIlPseudoType.Null)
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

            public IXamlIlType MakeGenericType(IReadOnlyList<IXamlIlType> typeArguments)
            {
                if (Reference == Definition)
                {
                    var i = Definition.MakeGenericInstanceType(typeArguments.Cast<ITypeReference>().Select(r => r.Reference)
                        .ToArray());
                    return TypeSystem.GetTypeFor(i);
                }
                throw new InvalidOperationException();
            }

            private IXamlIlType _genericTypeDefinition;

            public IXamlIlType GenericTypeDefinition =>
                _genericTypeDefinition ?? (_genericTypeDefinition =
                    (Reference is GenericInstanceType) ? TypeSystem.Resolve(Definition) : null);

            public bool IsArray => Reference.IsArray;

            private IXamlIlType _arrayType;

            public IXamlIlType ArrayElementType =>
                _arrayType ?? (_arrayType =
                    IsArray ? TypeSystem.Resolve(Definition) : null);

            public IXamlIlType MakeArrayType(int dimensions) => TypeSystem.Resolve(Reference.MakeArrayType(dimensions));

            private IXamlIlType _baseType;

            public IXamlIlType BaseType => Definition.BaseType == null
                ? null
                : _baseType ?? (_baseType = TypeSystem.Resolve(
                      Definition.BaseType.TransformGeneric(Reference)));
            
            public bool IsValueType => Definition.IsValueType;
            public bool IsEnum => Definition.IsEnum;
            protected IReadOnlyList<IXamlIlType> _interfaces;

            public IReadOnlyList<IXamlIlType> Interfaces =>
                _interfaces ?? (_interfaces =
                    Definition.Interfaces.Select(i => TypeSystem.Resolve(i.InterfaceType
                        .TransformGeneric(Reference))).ToList());
            
            public bool IsInterface => Definition.IsInterface;
            public IXamlIlType GetEnumUnderlyingType()
            {
                if (!IsEnum)
                    return null;
                return TypeSystem.Resolve(Definition.GetEnumUnderlyingType());
            }
            
            
        }
    }
}
