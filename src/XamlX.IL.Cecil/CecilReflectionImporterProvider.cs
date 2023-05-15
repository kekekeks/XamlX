using System;
using System.Reflection;
using Mono.Cecil;

namespace XamlX.TypeSystem;

internal class CecilReflectionImporterProvider : IReflectionImporterProvider
{
    public IReflectionImporter GetReflectionImporter(ModuleDefinition module)
    {
        return new ReflectionImporter(module);
    }

    public class ReflectionImporter : DefaultReflectionImporter
    {
        public ReflectionImporter(ModuleDefinition module) : base(module)
        {
        }

        public override TypeReference ImportReference(Type type, IGenericParameterProvider context)
        {
            throw new InvalidOperationException("Runtime reflection importer is not supported.");
        }

        public override MethodReference ImportReference(MethodBase method, IGenericParameterProvider context)
        {
            throw new InvalidOperationException("Runtime reflection importer is not supported.");
        }

        protected override IMetadataScope ImportScope(Type type)
        {
            throw new InvalidOperationException("Runtime reflection importer is not supported.");
        }

        public override FieldReference ImportReference(FieldInfo field, IGenericParameterProvider context)
        {
            throw new InvalidOperationException("Runtime reflection importer is not supported.");
        }

        public override AssemblyNameReference ImportReference(AssemblyName name)
        {
            throw new InvalidOperationException("Runtime reflection importer is not supported.");
        }
    }
}
