using Mono.Cecil;

namespace XamlIl.TypeSystem
{
    public partial class CecilTypeSystem
    {
        class UnresolvedCecilType : XamlIlPseudoType, ITypeReference
        {
            public TypeReference Reference { get; }

            public UnresolvedCecilType(TypeReference reference) : base("Unresolved:" + reference.FullName)
            {
                Reference = reference;
            }
        }
    }
}
