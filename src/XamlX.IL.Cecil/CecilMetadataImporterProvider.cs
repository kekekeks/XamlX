using XamlX.TypeSystem;
using Mono.Cecil;

namespace XamlX.TypeSystem;

internal class CecilMetadataImporterProvider : IMetadataImporterProvider
{
    private readonly CecilTypeSystem _typeSystem;

    public CecilMetadataImporterProvider(CecilTypeSystem typeSystem)
    {
        _typeSystem = typeSystem;
    }
            
    public IMetadataImporter GetMetadataImporter(ModuleDefinition module)
    {
        return new MetadataImporter(module, _typeSystem);
    }

    private class MetadataImporter : DefaultMetadataImporter
    {
        private readonly CecilTypeSystem _cecilTypeSystem;

        public MetadataImporter(ModuleDefinition module, CecilTypeSystem cecilTypeSystem) : base(module)
        {
            _cecilTypeSystem = cecilTypeSystem;
        }

        public override AssemblyNameReference ImportReference(AssemblyNameReference reference)
        {
            var coercedReference = _cecilTypeSystem.CoerceReference(reference);
            return base.ImportReference(coercedReference);
        }
    }
}
