using System;
using Mono.Cecil;

namespace XamlIl.TypeSystem
{
    public partial class CecilTypeSystem {
        public class CustomMetadataResolver : MetadataResolver
        {
            private readonly CecilTypeSystem _typeSystem;

            public CustomMetadataResolver(CecilTypeSystem typeSystem) : base(typeSystem)
            {
                _typeSystem = typeSystem;
            }

            public override TypeDefinition Resolve(TypeReference type)
            {
                type = type.GetElementType();
                var resolved = base.Resolve(type);
                if (resolved == null && type.Scope.MetadataScopeType == MetadataScopeType.AssemblyNameReference)
                {
                    return null;
                    var asm = _typeSystem.ResolveWrapped((AssemblyNameReference) type.Scope);
                    throw new Exception("TODO: process custom attrs");
                }
                else
                    return resolved;
            }
        }
    }
}