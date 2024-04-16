using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        internal class UnresolvedCecilType : XamlPseudoType, ITypeReference
        {
            public TypeReference Reference { get; }

            public UnresolvedCecilType(TypeReference reference) : base("Unresolved:" + reference.FullName)
            {
                Reference = reference;
            }
        }
    }
}
