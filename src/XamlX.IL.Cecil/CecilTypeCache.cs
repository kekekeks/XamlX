using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace XamlX.TypeSystem
{

    partial class CecilTypeSystem
    {
        class CecilTypeCache
        {
            public CecilTypeSystem TypeSystem { get; }
            
            Dictionary<TypeDefinition, DefinitionEntry> _definitions = new(new TypeDefinitionEqualityComparer());

            public CecilTypeCache(CecilTypeSystem typeSystem)
            {
                TypeSystem = typeSystem;
            }

            class DefinitionEntry
            {
                public CecilType Direct { get; set; }
                public Dictionary<Type, List<CecilType>> References { get; } = new Dictionary<Type, List<CecilType>>();
            }
            
            
            
            public CecilType Get(TypeReference reference)
            {
                if (reference.GetType() == typeof(TypeReference))
                    reference = reference.Resolve();
                else if (reference is RequiredModifierType modReqType)
                    reference = modReqType.ElementType;
                else if (reference is OptionalModifierType modOptType)
                    reference = modOptType.ElementType;

                var definition = reference.Resolve();
                var asm = TypeSystem.FindAsm(definition.Module.Assembly);
                if (!_definitions.TryGetValue(definition, out var dentry))
                    _definitions[definition] = dentry = new DefinitionEntry();
                if (reference is TypeDefinition def)
                    return dentry.Direct ?? (dentry.Direct = new CecilType(TypeSystem, asm, def));

                var rtype = reference.GetType();
                if (!dentry.References.TryGetValue(rtype, out var rlist))
                    dentry.References[rtype] = rlist = new List<CecilType>();
                var found = rlist.FirstOrDefault(t => TypeReferenceEqualityComparer.AreEqual(t.Reference, reference));
                if (found != null)
                    return found;
                var rv = new CecilType(TypeSystem, asm, definition, reference);
                rlist.Add(rv);
                return rv;
            }
        }
    }
}
