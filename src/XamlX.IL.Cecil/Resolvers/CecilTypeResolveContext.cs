using System.Collections.Generic;
using Mono.Cecil;

namespace XamlX.TypeSystem;

internal class CecilTypeResolveContext
{
    private readonly GenericTypeResolver _genericTypeResolver;
    private readonly Dictionary<TypeReference, IXamlType> _typeReferenceCache;
    public CecilTypeSystem TypeSystem { get; }

    public static CecilTypeResolveContext For(CecilTypeSystem typeSystem)
    {
        return new CecilTypeResolveContext(typeSystem, new GenericTypeResolver());
    }

    private CecilTypeResolveContext(
        CecilTypeSystem typeSystem,
        GenericTypeResolver genericTypeResolver,
        Dictionary<TypeReference, IXamlType> typeReferenceCache = null)
    {
        TypeSystem = typeSystem;
        _genericTypeResolver = genericTypeResolver;

        // Don't use CecilTypeComparisonMode.Exact on type cache, as we don't want to Resolve all types for that.
        // We don't mind some duplicates, if comes. 
        _typeReferenceCache = typeReferenceCache ?? new(new TypeReferenceEqualityComparer(
            CecilTypeComparisonMode.SignatureOnlyLoose));
    }

    public CecilTypeResolveContext Nested(TypeReference typeReference)
    {
        return new CecilTypeResolveContext(TypeSystem, _genericTypeResolver.Nested(typeReference, null), _typeReferenceCache);
    }

    public CecilTypeResolveContext Nested(MethodReference methodReference)
    {
        return new CecilTypeResolveContext(TypeSystem, _genericTypeResolver.Nested(null, methodReference), _typeReferenceCache);
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