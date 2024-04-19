using System.Collections.Generic;
using Mono.Cecil;

namespace XamlX.TypeSystem;

internal class CecilTypeResolveContext
{
    private readonly GenericTypeResolver _genericTypeResolver;
    private readonly CecilTypeCache _typeCache;
    public CecilTypeSystem TypeSystem { get; }

    public static CecilTypeResolveContext For(CecilTypeSystem typeSystem)
    {
        return new CecilTypeResolveContext(typeSystem, new GenericTypeResolver(), new CecilTypeCache());
    }

    private CecilTypeResolveContext(
        CecilTypeSystem typeSystem,
        GenericTypeResolver genericTypeResolver,
        CecilTypeCache typeCache)
    {
        TypeSystem = typeSystem;
        _genericTypeResolver = genericTypeResolver;
        _typeCache = typeCache;
    }

    public CecilTypeResolveContext Nested(TypeReference typeReference)
    {
        return new CecilTypeResolveContext(TypeSystem, _genericTypeResolver.Nested(typeReference, null), _typeCache);
    }

    public CecilTypeResolveContext Nested(MethodReference methodReference)
    {
        return new CecilTypeResolveContext(TypeSystem, _genericTypeResolver.Nested(null, methodReference), _typeCache);
    }

    public IXamlType Resolve(TypeReference reference, bool resolveGenerics = true)
    {
        // We ignore modopt/modreq to support get;init;, mathing behavior of SRE.
        if (reference is RequiredModifierType modReqType)
            reference = modReqType.ElementType;
        else if (reference is OptionalModifierType modOptType)
            reference = modOptType.ElementType;

        if (resolveGenerics)
        {
            reference = _genericTypeResolver.Resolve(reference);
        }

        return _typeCache.Resolve(this, reference);
    }

    public IXamlType ResolveReturnType(MethodReference method)
    {
        var type = _genericTypeResolver.Resolve(GenericParameterResolver.ResolveReturnTypeIfNeeded(method));
        return Resolve(type);
    }

    public IXamlType ResolveParameterType(MethodReference method, ParameterReference parameter)
    {
        var type = _genericTypeResolver.Resolve(GenericParameterResolver.ResolveParameterTypeIfNeeded(method, parameter));
        return Resolve(type);
    }

    public IXamlType ResolveParameterType(PropertyReference property, ParameterReference parameter)
    {
        var type = _genericTypeResolver.Resolve(GenericParameterResolver.ResolveParameterTypeIfNeeded(property, parameter));
        return Resolve(type);
    }

    public IXamlType ResolveFieldType(FieldReference field)
    {
        var type = _genericTypeResolver.Resolve(GenericParameterResolver.ResolveFieldTypeIfNeeded(field));
        return Resolve(type);
    }

    public IXamlType ResolvePropertyType(PropertyReference property)
    {
        var type = _genericTypeResolver.Resolve(GenericParameterResolver.ResolvePropertyTypeIfNeeded(property));
        return Resolve(type);
    }

    public MethodReference ResolveReference(MethodReference method, bool transformGenerics = true)
    {
        return _genericTypeResolver.Resolve(method, transformGenerics);
    }
    public TypeReference ResolveReference(TypeReference type)
    {
        return _genericTypeResolver.Resolve(type);
    }
    public FieldReference ResolveReference(FieldReference field)
    {
        return _genericTypeResolver.Resolve(field);
    }
}