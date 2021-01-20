using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace XamlX.TypeSystem
{
    internal class GenericTypeResolver
    {
        private readonly Dictionary<GenericParameter, TypeReference> lookup = new Dictionary<GenericParameter, TypeReference>();

        public void Scan(TypeReference r)
        {
            var typeRef = r;
            var typeDef = typeRef.Resolve();

            while (typeRef != null)
            {
                for (var i = 0; i < typeDef.GenericParameters.Count; ++i)
                {
                    if (typeDef.GenericParameters[i] is GenericParameter parameter &&
                        typeRef is GenericInstanceType genericTypeRef &&
                        genericTypeRef.GenericArguments.Count > i &&
                        genericTypeRef.GenericArguments[i] is TypeReference arg &&
                        !(arg is GenericParameter) &&
                        !lookup.ContainsKey(parameter))
                    {
                        lookup.Add(parameter, arg);
                    }
                }

                typeRef = typeDef.BaseType;
                typeDef = typeRef?.Resolve();
            }
        }

        public TypeReference Resolve(TypeReference r)
        {
            if (r is GenericInstanceType gi)
            {
                var args = new TypeReference[gi.GenericArguments.Count];

                for (var i = 0; i < gi.GenericArguments.Count; ++i)
                {
                    var sourceArg = gi.GenericArguments[i];

                    if (sourceArg is GenericParameter p)
                    {
                        if (lookup.TryGetValue(p, out var resolved))
                        {
                            args[i] = resolved;
                        }
                        else
                        {
                            throw new XamlTypeSystemException($"Could not resolve generic parameter '{p.Name}'.");
                        }
                    }
                    else
                    {
                        args[i] = Resolve(sourceArg);
                    }
                }

                return gi.ElementType.MakeGenericInstanceType(args);
            }
            else
            {
                return r;
            }
        }
    }
}
