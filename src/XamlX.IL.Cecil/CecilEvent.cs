using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilEvent : IXamlEventInfo
        {
            private readonly CecilTypeResolveContext _typeResolveContext;
            public EventDefinition Event { get; }

            public CecilEvent(CecilTypeResolveContext typeResolveContext, EventDefinition ev)
            {
                _typeResolveContext = typeResolveContext;
                Event = ev;
            }

            public string Name => Event.Name;

            private IXamlMethod? _add;

            public IXamlMethod? Add => Event.AddMethod == null
                ? null
                : _add ??= new CecilMethod(_typeResolveContext, Event.AddMethod);

            private IXamlType? _declaringType;

            public IXamlType DeclaringType
                => _declaringType ??= _typeResolveContext.Resolve(Event.DeclaringType);

            public bool Equals(IXamlEventInfo? other) =>
                other is CecilEvent cf
                && TypeReferenceEqualityComparer.AreEqual(Event.DeclaringType, cf.Event.DeclaringType, CecilTypeComparisonMode.Exact)
                && cf.Event.FullName == Event.FullName;

            public override bool Equals(object? other) => Equals(other as IXamlEventInfo);

            public override int GetHashCode() =>
                (TypeReferenceEqualityComparer.GetHashCodeFor(Event.DeclaringType, CecilTypeComparisonMode.Exact), Event.FullName).GetHashCode();

            public override string ToString() => Event.ToString();
        }
    }
}