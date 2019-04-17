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
            
            public string Name => Reference.Name;
            public bool IsPublic => Definition.IsPublic;
            public bool IsStatic => Definition.IsStatic;
            private IXamlType _returnType;

            
            
            public IXamlType ReturnType =>
                _returnType ?? (_returnType = TypeSystem.Resolve(Reference.ReturnType));

            private IXamlType _declaringType;

            public IXamlType DeclaringType =>
                _declaringType = _declaringType ?? (_declaringType = TypeSystem.Resolve(_declaringTypeReference));

            private IReadOnlyList<IXamlType> _parameters;

            public IReadOnlyList<IXamlType> Parameters =>
                _parameters ?? (_parameters =
                    Reference.Parameters.Select(p => TypeSystem.Resolve(p.ParameterType)).ToList());

            private IXamlILEmitter _generator;

            public IXamlILEmitter Generator =>
                _generator ?? (_generator = new CecilEmitter(TypeSystem, Definition.Body));
        }

        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        class CecilMethod : CecilMethodBase, IXamlMethodBuilder
        {
            public CecilMethod(CecilTypeSystem typeSystem, MethodDefinition methodDef,
                TypeReference declaringType) : base(typeSystem, methodDef, declaringType)
            {
            }

            public bool Equals(IXamlMethod other) => other is CecilMethod cm
                                                       && cm.Reference.Equals(Reference);

            
        }
        
        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        class CecilConstructor : CecilMethodBase, IXamlConstructorBuilder
        {
            public CecilConstructor(CecilTypeSystem typeSystem, MethodDefinition methodDef,
                TypeReference declaringType) : base(typeSystem, methodDef, declaringType)
            {
            }

            public bool Equals(IXamlConstructor other) => other is CecilConstructor cm
                                                            && cm.Reference.Equals(Reference);
        }

        class UnresolvedMethod : IXamlMethod
        {
            public UnresolvedMethod(string name)
            {
                Name = name;
            }
            
            public bool Equals(IXamlMethod other) => other == this;

            public string Name { get; }
            public bool IsPublic { get; }
            public bool IsStatic { get; }
            public IXamlType ReturnType { get; } = XamlPseudoType.Unknown;
            public IReadOnlyList<IXamlType> Parameters { get; } = new IXamlType[0];
            public IXamlType DeclaringType { get; } = XamlPseudoType.Unknown;
        }
    }
}
