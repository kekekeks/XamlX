using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilMethodBase
        {
            public CecilTypeSystem TypeSystem { get; }
            public MethodReference Reference { get; }
            public MethodReference IlReference { get; }
            public MethodDefinition Definition { get; }
            private TypeReference _declaringTypeReference;
            

            public CecilMethodBase(CecilTypeSystem typeSystem, MethodDefinition method, TypeReference declaringType)
            {
                TypeSystem = typeSystem;

                MethodReference MakeRef(bool transform)
                {
                    TypeReference Transform(TypeReference r) => transform ? r.TransformGeneric(declaringType) : r;
                    
                    var reference = new MethodReference(method.Name, Transform(method.ReturnType),
                        declaringType)
                    {
                        HasThis = method.HasThis,
                        ExplicitThis = method.ExplicitThis,
                    };
                    foreach (ParameterDefinition parameter in method.Parameters)
                        reference.Parameters.Add(
                            new ParameterDefinition(Transform(parameter.ParameterType)));

                    foreach (var genericParam in method.GenericParameters)
                        reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
                    return reference;
                }

                Reference = MakeRef(true);
                IlReference = MakeRef(false);
                Definition = method;
                _declaringTypeReference = declaringType;
            }

            public CecilMethodBase(CecilTypeSystem typeSystem, GenericInstanceMethod method)
            {
                TypeSystem = typeSystem;
                Reference = method;
                IlReference = method.ElementMethod;
                Definition = method.ElementMethod.Resolve();
                _declaringTypeReference = Definition.DeclaringType;
            }
            
            public string Name => Reference.Name;
            public bool IsPublic => Definition.IsPublic;
            public bool IsStatic => Definition.IsStatic;
            private IXamlXType _returnType;

            
            
            public IXamlXType ReturnType =>
                _returnType ?? (_returnType = TypeSystem.Resolve(Reference.ReturnType));

            private IXamlXType _declaringType;

            public IXamlXType DeclaringType =>
                _declaringType = _declaringType ?? (_declaringType = TypeSystem.Resolve(_declaringTypeReference));

            private IReadOnlyList<IXamlXType> _parameters;

            public IReadOnlyList<IXamlXType> Parameters =>
                _parameters ?? (_parameters =
                    Reference.Parameters.Select(p => TypeSystem.Resolve(p.ParameterType)).ToList());

            private IXamlXEmitter _generator;

            public IXamlXEmitter Generator =>
                _generator ?? (_generator = new CecilEmitter(TypeSystem, Definition));
        }

        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        class CecilMethod : CecilMethodBase, IXamlXMethodBuilder
        {
            public CecilMethod(CecilTypeSystem typeSystem, MethodDefinition methodDef,
                TypeReference declaringType) : base(typeSystem, methodDef, declaringType)
            {
            }

            private CecilMethod(CecilTypeSystem typeSystem, GenericInstanceMethod method)
                : base(typeSystem, method)
            {
            }

            public bool Equals(IXamlXMethod other) =>
                // I hope this is enough...
                other is CecilMethod cm
                && DeclaringType.Equals(cm.DeclaringType)
                && Reference.FullName == cm.Reference.FullName;

            public IXamlXMethod MakeGenericMethod(IReadOnlyList<IXamlXType> typeArguments)
            {
                GenericInstanceMethod instantiation = new GenericInstanceMethod(Reference);
                if (Reference == Definition)
                {
                    foreach (var type in typeArguments.Cast<ITypeReference>().Select(r => r.Reference))
                    {
                        instantiation.GenericArguments.Add(type);
                    }
                }

                return new CecilMethod(TypeSystem, instantiation);
            }
        }
        
        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        class CecilConstructor : CecilMethodBase, IXamlXConstructorBuilder
        {
            public CecilConstructor(CecilTypeSystem typeSystem, MethodDefinition methodDef,
                TypeReference declaringType) : base(typeSystem, methodDef, declaringType)
            {
            }

            public bool Equals(IXamlXConstructor other) => other is CecilConstructor cm
                                                            && cm.Reference.Equals(Reference);
        }

        class UnresolvedMethod : IXamlXMethod
        {
            public UnresolvedMethod(string name)
            {
                Name = name;
            }
            
            public bool Equals(IXamlXMethod other) => other == this;

            public IXamlXMethod MakeGenericMethod(IReadOnlyList<IXamlXType> typeArguments)
            {
                return new UnresolvedMethod(Name);
            }

            public string Name { get; }
            public bool IsPublic { get; }
            public bool IsStatic { get; }
            public IXamlXType ReturnType { get; } = XamlXPseudoType.Unknown;
            public IReadOnlyList<IXamlXType> Parameters { get; } = new IXamlXType[0];
            public IXamlXType DeclaringType { get; } = XamlXPseudoType.Unknown;
        }
    }
}
