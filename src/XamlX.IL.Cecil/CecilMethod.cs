using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using XamlX.IL;

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
            protected readonly TypeReference _declaringTypeReference;

            public CecilMethodBase(CecilTypeSystem typeSystem, MethodReference method, TypeReference declaringType)
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

                    if (method is GenericInstanceMethod generic)
                    {
                        var genericReference = new GenericInstanceMethod(reference);
                        foreach (var genericArg in generic.GenericArguments)
                        {
                            genericReference.GenericArguments.Add(Transform(genericArg));
                        }
                        reference = genericReference;
                    }

                    return reference;
                }

                Reference = MakeRef(true);
                IlReference = MakeRef(false);
                Definition = method.Resolve();
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
            
            private IReadOnlyList<IXamlCustomAttribute> _attributes;
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ?? (_attributes =
                    Definition.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeSystem, ca)).ToList());

            private IXamlILEmitter _generator;

            public IXamlILEmitter Generator =>
                _generator ?? (_generator = new CecilEmitter(TypeSystem, Definition));
        }

        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        class CecilMethod : CecilMethodBase, IXamlMethodBuilder<IXamlILEmitter>
        {
            public CecilMethod(CecilTypeSystem typeSystem, MethodReference methodRef,
                TypeReference declaringType) : base(typeSystem, methodRef, declaringType)
            {
            }

            public bool Equals(IXamlMethod other) =>
                // I hope this is enough...
                other is CecilMethod cm
                && DeclaringType.Equals(cm.DeclaringType)
                && Reference.FullName == cm.Reference.FullName;

            public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments)
            {
                GenericInstanceMethod instantiation = new GenericInstanceMethod(Reference);
                foreach (var type in typeArguments.Cast<ITypeReference>().Select(r => r.Reference))
                {
                    instantiation.GenericParameters.Add(new GenericParameter(Reference));
                    instantiation.GenericArguments.Add(type);
                }

                return new CecilMethod(TypeSystem, instantiation, _declaringTypeReference);
            }
        }
        
        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        class CecilConstructor : CecilMethodBase, IXamlConstructorBuilder<IXamlILEmitter>
        {
            public CecilConstructor(CecilTypeSystem typeSystem, MethodDefinition methodDef,
                TypeReference declaringType) : base(typeSystem, methodDef, declaringType)
            {
            }

            public bool Equals(IXamlConstructor other) => other is CecilConstructor cm
                                                            && cm.Reference.Equals(Reference);
        }
    }
}
