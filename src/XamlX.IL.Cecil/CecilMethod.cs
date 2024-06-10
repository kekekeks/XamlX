using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using XamlX.IL;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilParameterInfo : IXamlParameterInfo
        {
            private readonly CecilTypeResolveContext _typeResolveContext;
            private readonly ParameterReference _parameterReference;

            public CecilParameterInfo(CecilTypeResolveContext typeResolveContext, ParameterReference parameterReference)
            {
                _typeResolveContext = typeResolveContext;
                _parameterReference = parameterReference;
            }

            public string Name => _parameterReference.Name;

            private IXamlType? _parameterType;
            public IXamlType ParameterType => _parameterType ??= _typeResolveContext.Resolve(_parameterReference.ParameterType);
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _parameterReference.Resolve().CustomAttributes
                .Select(ca => new CecilCustomAttribute(_typeResolveContext, ca)).ToList();
        }

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

            private IXamlType? _returnType;
            
            public IXamlType ReturnType =>
                _returnType ??= TypeResolveContext.ResolveReturnType(Reference);

            private IXamlType? _declaringType;

            public IXamlType DeclaringType =>
                _declaringType ??= TypeResolveContext.Resolve(Reference.DeclaringType);

            public IReadOnlyList<IXamlType> Parameters => ParameterInfos.Select(p => p.ParameterType).ToList();

            private IReadOnlyList<IXamlCustomAttribute>? _attributes;

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
                _attributes ??= Definition.CustomAttributes.Select(ca => new CecilCustomAttribute(TypeResolveContext, ca)).ToList();

            private IXamlILEmitter? _generator;

            public IXamlILEmitter Generator =>
                _generator ??= new CecilEmitter(TypeResolveContext.TypeSystem, Definition);

            public IXamlParameterInfo GetParameterInfo(int index) => ParameterInfos[index];

            private IReadOnlyList<IXamlParameterInfo>? _parameterInfos;
            private IReadOnlyList<IXamlParameterInfo> ParameterInfos =>
                _parameterInfos ??= Reference.Parameters.Select(p => new CecilParameterInfo(TypeResolveContext, p)).ToList();

            public override string ToString() => Definition.ToString();
        }

        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        sealed class CecilMethod : CecilMethodBase, IXamlMethodBuilder<IXamlILEmitter>
        {
            private IReadOnlyList<IXamlType>? _genericParameters;
            private IReadOnlyList<IXamlType>? _genericArguments;

            public bool IsGenericMethod => Reference.HasGenericParameters;
            public bool IsGenericMethodDefinition => Reference.IsDefinition && Reference.HasGenericParameters;

            public IReadOnlyList<IXamlType> GenericParameters => _genericParameters ??=
                !Reference.IsGenericInstance && Reference.ContainsGenericParameter
                ? Reference.GenericParameters
                    .Select(gp => TypeResolveContext.Resolve(gp))
                    .ToArray() ?? System.Array.Empty<IXamlType>()
                : System.Array.Empty<IXamlType>();

            public IReadOnlyList<IXamlType> GenericArguments => _genericArguments ??=
                 Reference.IsGenericInstance
                 ? ((GenericInstanceMethod)Reference).GenericArguments.Select(ga=> TypeResolveContext.Resolve(ga))?.ToArray() 
                        ?? System.Array.Empty<IXamlType>()
                    : System.Array.Empty<IXamlType>();

            public bool ContainsGenericParameters => !Reference.IsGenericInstance && Reference.ContainsGenericParameter;

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

            public bool Equals(IXamlMethod? other) =>
                other is CecilMethod cm
                && MethodReferenceEqualityComparer.AreEqual(Reference, cm.Reference, CecilTypeComparisonMode.Exact);

            public override bool Equals(object? other) => Equals(other as IXamlMethod);

            public override int GetHashCode()
                => MethodReferenceEqualityComparer.GetHashCodeFor(Reference, CecilTypeComparisonMode.Exact);
        }

        [DebuggerDisplay("{" + nameof(Reference) + "}")]
        sealed class CecilConstructor : CecilMethodBase, IXamlConstructorBuilder<IXamlILEmitter>
        {
            public CecilConstructor(CecilTypeResolveContext typeResolveContext, MethodDefinition methodDef)
                : base(typeResolveContext, methodDef)
            {
            }

            public bool Equals(IXamlConstructor? other) =>
                other is CecilConstructor cm
                && MethodReferenceEqualityComparer.AreEqual(Reference, cm.Reference, CecilTypeComparisonMode.Exact);

            public override bool Equals(object? other) => Equals(other as IXamlConstructor);

            public override int GetHashCode()
                => MethodReferenceEqualityComparer.GetHashCodeFor(Reference, CecilTypeComparisonMode.Exact);
        }
    }
}
