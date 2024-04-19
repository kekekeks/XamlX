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
            protected CecilTypeResolveContext TypeResolveContext { get; }
            public MethodReference Reference { get; }
            public MethodReference IlReference { get; }
            public MethodDefinition Definition { get; }

            public CecilMethodBase(CecilTypeResolveContext typeResolveContext, MethodReference method)
            {
                Reference = typeResolveContext.ResolveReference(method);
                IlReference = typeResolveContext.ResolveReference(method, transformGenerics: false);
                Definition = method.Resolve();
                TypeResolveContext = typeResolveContext.Nested(Reference);
            }
            
            public string Name => Reference.Name;
            public bool IsPublic => Definition.IsPublic;
            public bool IsPrivate => Definition.IsPrivate;
            public bool IsFamily => Definition.IsFamily;
            public bool IsStatic => Definition.IsStatic;

            private IXamlType _returnType;
            
            public IXamlType ReturnType =>
                _returnType ??= TypeResolveContext.ResolveReturnType(Reference);

            private IXamlType _declaringType;

            public IXamlType DeclaringType =>
                _declaringType ??= TypeResolveContext.Resolve(Reference.DeclaringType);

            private IReadOnlyList<IXamlType> _parameters;

            public IReadOnlyList<IXamlType> Parameters =>
                _parameters ??= Reference.Parameters.Select(p => TypeResolveContext.ResolveParameterType(Reference, p)).ToList();
            
            private IReadOnlyList<IXamlCustomAttribute> _attributes;

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ??= Definition.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeResolveContext, ca)).ToList();

            private IXamlILEmitter _generator;

            public IXamlILEmitter Generator =>
                _generator ??= new CecilEmitter(TypeResolveContext.TypeSystem, Definition);

            public override string ToString() => Definition.ToString();
        }

        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        sealed class CecilMethod : CecilMethodBase, IXamlMethodBuilder<IXamlILEmitter>
        {
            public CecilMethod(CecilTypeResolveContext typeResolveContext, MethodReference method)
                : base(typeResolveContext, method)
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

                return new CecilMethod(TypeResolveContext, instantiation);
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
            public CecilConstructor(CecilTypeResolveContext typeResolveContext, MethodDefinition methodDef)
                : base(typeResolveContext, methodDef)
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
