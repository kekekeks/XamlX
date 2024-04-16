using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using XamlX.IL;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        internal abstract class CecilMethodBase
        {
            protected CecilTypeResolver TypeResolver { get; }
            public MethodReference Reference { get; }
            public MethodReference IlReference { get; }
            public MethodDefinition Definition { get; }

            public CecilMethodBase(CecilTypeResolver typeResolver, MethodReference method)
            {
                MethodReference MakeIlRef()
                {
                    var reference = new MethodReference(method.Name, method.ReturnType, Reference.DeclaringType)
                    {
                        HasThis = method.HasThis,
                        ExplicitThis = method.ExplicitThis,
                    };

                    foreach (ParameterDefinition parameter in method.Parameters)
                        reference.Parameters.Add(
                            new ParameterDefinition(parameter.ParameterType));

                    foreach (var genericParam in method.GenericParameters)
                        reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));

                    if (method is GenericInstanceMethod generic)
                    {
                        var genericReference = new GenericInstanceMethod(reference);
                        foreach (var genericArg in generic.GenericArguments)
                        {
                            genericReference.GenericArguments.Add(genericArg);
                        }
                        reference = genericReference;
                    }

                    return reference;
                }

                Reference = typeResolver.ResolveReference(method);
                IlReference = MakeIlRef();
                Definition = method.Resolve();
                TypeResolver = typeResolver.Nested(Reference);
            }
            
            public string Name => Reference.Name;
            public bool IsPublic => Definition.IsPublic;
            public bool IsPrivate => Definition.IsPrivate;
            public bool IsFamily => Definition.IsFamily;
            public bool IsStatic => Definition.IsStatic;

            private IXamlType _returnType;
            
            public IXamlType ReturnType =>
                _returnType ??= TypeResolver.ResolveReturnType(Reference);

            private IXamlType _declaringType;

            public IXamlType DeclaringType =>
                _declaringType ??= TypeResolver.Resolve(Reference.DeclaringType);

            private IReadOnlyList<IXamlType> _parameters;

            public IReadOnlyList<IXamlType> Parameters =>
                _parameters ??= Reference.Parameters.Select(p => TypeResolver.ResolveParameterType(Reference, p)).ToList();
            
            private IReadOnlyList<IXamlCustomAttribute> _attributes;

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ??= Definition.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeResolver, ca)).ToList();

            private IXamlILEmitter _generator;

            public IXamlILEmitter Generator =>
                _generator ??= new CecilEmitter(TypeResolver.TypeSystem, Definition);

            public override string ToString() => Definition.ToString();
        }

        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        sealed class CecilMethod : CecilMethodBase, IXamlMethodBuilder<IXamlILEmitter>
        {
            public CecilMethod(CecilTypeResolver typeResolver, MethodReference method)
                : base(typeResolver, method)
            {
            }

            public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments)
            {
                GenericInstanceMethod instantiation = new GenericInstanceMethod(Reference);
                foreach (var type in typeArguments.Cast<ITypeReference>().Select(r => r.Reference))
                {
                    instantiation.GenericParameters.Add(new GenericParameter(Reference));
                    instantiation.GenericArguments.Add(type);
                }

                return new CecilMethod(TypeResolver, instantiation);
            }

            public bool Equals(IXamlMethod other) =>
                other is CecilMethod cm
                && MethodReferenceEqualityComparer.AreEqual(Reference, cm.Reference);

            public override bool Equals(object other) => Equals(other as IXamlMethod);

            public override int GetHashCode() 
                => MethodReferenceEqualityComparer.GetHashCodeFor(Reference);
        }
        
        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        sealed class CecilConstructor : CecilMethodBase, IXamlConstructorBuilder<IXamlILEmitter>
        {
            public CecilConstructor(CecilTypeResolver typeResolver, MethodDefinition methodDef)
                : base(typeResolver, methodDef)
            {
            }

            public bool Equals(IXamlConstructor other) =>
                other is CecilConstructor cm
                && MethodReferenceEqualityComparer.AreEqual(Reference, cm.Reference);

            public override bool Equals(object other) => Equals(other as IXamlConstructor);

            public override int GetHashCode() 
                => MethodReferenceEqualityComparer.GetHashCodeFor(Reference);
        }
    }
}
