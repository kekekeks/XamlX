using System.Collections.Generic;
using Mono.Cecil;

namespace XamlX.TypeSystem;

internal class CecilTypeResolver
{
    private readonly GenericTypeResolver _genericTypeResolver;
    private readonly Dictionary<TypeReference, IXamlType> _typeReferenceCache;
    public CecilTypeSystem TypeSystem { get; }

    public static CecilTypeResolver For(CecilTypeSystem typeSystem)
    {
        return new CecilTypeResolver(typeSystem, new GenericTypeResolver());
    }

    private CecilTypeResolver(
        CecilTypeSystem typeSystem,
        GenericTypeResolver genericTypeResolver,
        Dictionary<TypeReference, IXamlType> typeReferenceCache = null)
    {
        TypeSystem = typeSystem;
        _genericTypeResolver = genericTypeResolver;
        _typeReferenceCache = typeReferenceCache ?? new(new TypeReferenceEqualityComparer());
    }

    public CecilTypeResolver Nested(TypeReference typeReference)
    {
        return new CecilTypeResolver(TypeSystem, _genericTypeResolver.Nested(typeReference, null), _typeReferenceCache);
    }

    public CecilTypeResolver Nested(MethodReference methodReference)
    {
        return new CecilTypeResolver(TypeSystem, _genericTypeResolver.Nested(null, methodReference), _typeReferenceCache);
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

        if (!_typeReferenceCache.TryGetValue(reference, out var rv))
        {
            TypeDefinition definition = null;
            try
            {
                definition = reference.Resolve();
            }
            catch (AssemblyResolutionException)
            {

            }

            if (definition != null)
            {
                var asm = TypeSystem.FindAsm(definition.Module.Assembly);
                rv = new CecilTypeSystem.CecilType(this, asm, definition, reference);
            }
            else
            {
                rv = new CecilTypeSystem.UnresolvedCecilType(reference);
            }
            _typeReferenceCache[reference] = rv;
        }
        return rv;
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

    public MethodReference ResolveReference(MethodReference method)
    {
        return _genericTypeResolver.Resolve(method);
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