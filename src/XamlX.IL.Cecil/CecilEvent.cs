using System.Collections.Generic;
using Mono.Cecil;
using XamlX.TypeSystem;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilEvent : IXamlEventInfo
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
            public bool Equals(IXamlEventInfo other) => other is CecilEvent cp && cp.Event == Event;
            public string Name => Event.Name;

            private IXamlMethod _getter;

            public IXamlMethod Add => Event.AddMethod == null
                ? null
                : _getter ?? (_getter = TypeSystem.Resolve(Event.AddMethod, _declaringType));


        }
    }
}