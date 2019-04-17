using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class UnresolvedCecilType : XamlPseudoType, ITypeReference
        {
            public TypeReference Reference { get; }

            public UnresolvedCecilType(TypeReference reference) : base("Unresolved:" + reference.FullName)
            {
                Reference = reference;
            }
        }
    }
}
