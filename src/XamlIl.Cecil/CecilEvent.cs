using System.Collections.Generic;
using Mono.Cecil;
using XamlIl.TypeSystem;

namespace XamlIl.TypeSystem
{
    public partial class CecilTypeSystem
    {
        class CecilEvent : IXamlIlEventInfo
        {
            private readonly TypeReference _declaringType;
            public CecilTypeSystem TypeSystem { get; }
            public EventDefinition Event { get; }

            public CecilEvent(CecilTypeSystem typeSystem, EventDefinition ev, TypeReference declaringType)
            {
                _declaringType = declaringType;
                TypeSystem = typeSystem;
                Event = ev;
            }
            public bool Equals(IXamlIlEventInfo other) => other is CecilEvent cp && cp.Event == Event;
            public string Name => Event.Name;

            private IXamlIlMethod _getter;

            public IXamlIlMethod Add => Event.AddMethod == null
                ? null
                : _getter ?? (_getter = TypeSystem.Resolve(Event.AddMethod, _declaringType));


        }
    }
}