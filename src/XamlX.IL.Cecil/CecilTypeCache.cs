using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem;

internal class CecilTypeCache
{
    // Don't use comparer here. Reference based GetHashCode is expected for TypeReference.
    // To avoid IXamlType duplicates we have second layer of TypeDefinition cache.
    private readonly Dictionary<TypeReference, IXamlType> _typeReferenceCache = new();

    private readonly Dictionary<TypeDefinition, DefinitionEntry> _definitions =
        new();

    private class DefinitionEntry
    {
        public Dictionary<MetadataType, List<CecilTypeSystem.CecilType>> References { get; } = new();
    }

    public IXamlType Resolve(CecilTypeResolveContext resolveContext, TypeReference reference)
    {
        if (!_typeReferenceCache.TryGetValue(reference, out var rv))
        {
            TypeDefinition? definition = null;
            try
            {
                definition = reference.Resolve();
            }
            catch (AssemblyResolutionException)
            {

            }

            if (definition != null)
            {
                rv = SecondLayerCache(resolveContext, reference, definition);
            }
            // For a function pointer, definition will always be null, as function pointers never have any TypeDefinition.
            else if (reference is FunctionPointerType functionPointerType)
            {
                rv = new CecilTypeSystem.CecilFunctionPointerType(functionPointerType);
            }
            else
            {
                rv = new CecilTypeSystem.UnresolvedCecilType(reference);
            }

            _typeReferenceCache[reference] = rv;
        }

        return rv;
    }

    private CecilTypeSystem.CecilType SecondLayerCache(
        CecilTypeResolveContext resolveContext,
        TypeReference reference,
        TypeDefinition definition)
    {
        var asm = resolveContext.TypeSystem.FindAsm(definition.Module.Assembly);

        // `_typeReferenceCache.TryGetValue` might result in duplicates, as the same Type can have different resolving modules.
        if (!_definitions.TryGetValue(definition, out var dentry))
            _definitions[definition] = dentry = new DefinitionEntry();

        // Per single TypeDefinition, there could be multiple TypeReference with different signature and different types.
        var metadataType = reference.MetadataType;
        if (!dentry.References.TryGetValue(metadataType, out var rlist))
            dentry.References[metadataType] = rlist = new List<CecilTypeSystem.CecilType>();

        // If we previously had cached TypeDefinition+TypeReference with the same signature - return existing. 
        var found = rlist.FirstOrDefault(t =>
            TypeReferenceEqualityComparer.AreEqual(t.Reference, reference, CecilTypeComparisonMode.SignatureOnlyLoose));
        if (found != null)
            return found;

        // Otherwise create and cache a new one.
        var cecilType = new CecilTypeSystem.CecilType(resolveContext, asm, definition, reference);
        rlist.Add(cecilType);

        return cecilType;
    }
}