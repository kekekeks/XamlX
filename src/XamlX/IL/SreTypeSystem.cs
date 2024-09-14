using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using XamlX.TypeSystem;
#if !XAMLX_NO_SRE
namespace XamlX.IL
{
    [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
#if !XAMLX_INTERNAL
    public
#endif
    class SreTypeSystem : IXamlTypeSystem
    {
        private List<IXamlAssembly> _assemblies = new List<IXamlAssembly>();
        public IEnumerable<IXamlAssembly> Assemblies => EnumerateList(_assemblies);

        private Dictionary<Type, SreType> _typeDic = new Dictionary<Type, SreType>();

        public SreTypeSystem()
        {
            // Ensure that System.ComponentModel is available
            var rasm = typeof(ISupportInitialize).Assembly;
            rasm = typeof(ITypeDescriptorContext).Assembly;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                try
                {
                    ResolveAssembly(asm);
                }
                catch
                {
                    //
                }
        }
        
        public IXamlAssembly? FindAssembly(string name)
        {
            return Assemblies.FirstOrDefault(a => a.Name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        SreAssembly? ResolveAssembly(Assembly asm)
        {
            if (asm.IsDynamic)
                return null;
            foreach (var a in Assemblies)
                if (((SreAssembly)a).Assembly == asm)
                    return (SreAssembly)a;
            var n = new SreAssembly(this, asm);
            _assemblies.Add(n);
            n.Init();
            return n;
        }

        [return: NotNullIfNotNull(nameof(t))]
        SreType? ResolveType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? t)
        {
            if (t is null)
                return null;
            if (_typeDic.TryGetValue(t, out var rv))
                return rv;
            _typeDic[t] = rv = new SreType(this, ResolveAssembly(t.Assembly), t);
            return rv;
        }

        public IXamlType? FindType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string name, string? asm)
        {
            if (asm != null)
                name += ", " + asm;
            var found = Type.GetType(name);
            if (found == null)
                return null;
            return ResolveType(found);
        }

        public IXamlType? FindType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string name)
        {
            foreach (var asm in Assemblies)
            {
                var t = asm.FindType(name);
                if (t != null)
                    return t;
            }

            return null;
        }

        private static IEnumerable<T> EnumerateList<T>(IList<T> list)
        {
            for (var c = 0; c < list.Count; c++)
                yield return list[c];
        }

        class SreAssembly : IXamlAssembly
        {
            private readonly SreTypeSystem _system;
            private IReadOnlyList<IXamlCustomAttribute>? _customAttributes;
            public Assembly Assembly { get; }

            public SreAssembly(SreTypeSystem system, Assembly asm)
            {
                _system = system;
                Assembly = asm;
            }


            public bool Equals(IXamlAssembly? other)
                => other is SreAssembly typedOther && Assembly == typedOther.Assembly;

            public string Name => Assembly.GetName().Name ?? string.Empty;

            public IReadOnlyList<IXamlType> Types { get; private set; } = [];
            private Dictionary<string, SreType> _typeDic = [];

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TrimmedAttributes)]
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes
                => _customAttributes ??= Assembly.GetCustomAttributesData().Select(a => new SreCustomAttribute(
                    _system, a, _system.ResolveType(a.AttributeType))).ToList();

            public IXamlType? FindType(string fullName)
            {
                _typeDic.TryGetValue(fullName, out var rv);
                return rv;
            }

            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.CanBeSafelyTrimmed)]
            [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = TrimmingMessages.CanBeSafelyTrimmed)]
            [RequiresUnreferencedCode("DynamicallyAccessedMembers")]
            public void Init()
            {
                var allTypes = Assembly.GetTypes();
                for (var tIndex = 0; tIndex < allTypes.Length; tIndex++)
                {
                    var t = allTypes[tIndex];
                    var isVisbible = t.IsPublic
                          || t.IsTopLevelInternal()
                          || t.IsNestedlPublic_Or_Internal();
                    if (isVisbible)
                    {
                        if (t.DeclaringType is null)
                        {
                            var x = _system!.ResolveType(t);
                            _typeDic.Add(t.FullName!, x);
                        }
                        else
                        {
                            if (_typeDic.ContainsKey(t.DeclaringType.FullName!))
                            {
                                var x = _system!.ResolveType(t);
                                _typeDic.Add(t.FullName!, x);
                            }
                        }
                    }
                }
                Types = _typeDic.Values.ToArray();
            }
        }

        class SreMemberInfo
        {
            protected readonly SreTypeSystem System;
            private readonly MemberInfo _member;
            private IReadOnlyList<IXamlCustomAttribute>? _customAttributes;

            public string Name => _member.Name;

            public SreMemberInfo(SreTypeSystem system, MemberInfo member)
            {
                System = system;
                _member = member;
            }

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TrimmedAttributes)]
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes
                => _customAttributes ??= _member.GetCustomAttributesData().Select(a => new SreCustomAttribute(System,
                    a, System.ResolveType(a.AttributeType))).ToList();
        }

        [DebuggerDisplay("{" + nameof(Type) + "}")]
        class SreType : SreMemberInfo, IXamlType
        {
            private IReadOnlyList<IXamlProperty>? _properties;
            private IReadOnlyList<IXamlField>? _fields;
            private IReadOnlyList<IXamlMethod>? _methods;
            private IReadOnlyList<IXamlConstructor>? _constructors;
            private IReadOnlyList<IXamlType>? _genericArguments;
            private IReadOnlyList<IXamlType>? _genericParameters;
            private IReadOnlyList<IXamlType>? _interfaces;
            private IReadOnlyList<IXamlEventInfo>? _events;
            private IXamlType? _baseType;
            private IXamlType? _declaringType;
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] public Type Type { get; }

            public SreType(SreTypeSystem system, SreAssembly? asm, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type): base(system, type)
            {
                Assembly = asm;
                Type = type;
            }

            public bool Equals(IXamlType? other) => other is SreType typedOther && Type == typedOther.Type;
            public override int GetHashCode() => Type.GetHashCode();
            public object Id => Type;

            public string FullName => Type.FullName!;
            public string? Namespace => Type.Namespace;
            public bool IsPublic => Type.IsPublic;
            public bool IsNestedPrivate => Type.IsNestedPrivate;
            public IXamlAssembly? Assembly { get; }

            public IReadOnlyList<IXamlProperty> Properties =>
                _properties ??= Type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Select(p => new SreProperty(System, p)).ToList();

            public IReadOnlyList<IXamlField> Fields =>
                _fields ??= Type.GetFields(BindingFlags.Public | BindingFlags.Static
                                                               | BindingFlags.Instance |
                                                               BindingFlags.NonPublic
                                                               | BindingFlags.DeclaredOnly)
                    .Select(f => new SreField(System, f)).ToList();

            public IReadOnlyList<IXamlMethod> Methods =>
                _methods ??= Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                             BindingFlags.Static |
                                             BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(m => new SreMethod(System, m)).ToList();

            public IReadOnlyList<IXamlConstructor> Constructors =>
                _constructors ??= Type.GetConstructors(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(c => new SreConstructor(System, c)).ToList();

            public IReadOnlyList<IXamlType> Interfaces =>
                _interfaces ??= Type.GetInterfaces().Select(System.ResolveType).ToList()!;

            public IReadOnlyList<IXamlEventInfo> Events =>
                _events ??= Type
                    .GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                    .Select(e => new SreEvent(System, e)).ToList();

            public bool IsInterface => Type.IsInterface;

            public IReadOnlyList<IXamlType> GenericArguments
            {
                get
                {
                    if (_genericArguments != null)
                        return _genericArguments;
                    if (GenericTypeDefinition == null)
                        return _genericArguments = [];
                    return _genericArguments = Type.GetGenericArguments().Select(System.ResolveType).ToList()!;
                }
            }

            public IReadOnlyList<IXamlType> GenericParameters =>
                _genericParameters ??= Type.GetTypeInfo().GenericTypeParameters.Select(System.ResolveType).ToList()!;

            public bool IsAssignableFrom(IXamlType type)
            {
                if (type == XamlPseudoType.Null)
                {
                    if (!Type.IsValueType)
                        return true;
                    if (Type.IsConstructedGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return true;
                    return false;
                }

                if (type is SreType sreType)
                    return Type.IsAssignableFrom((sreType).Type);
                return false;
            }

            [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments)
            {
                return System.ResolveType(
                    Type.MakeGenericType(typeArguments.Select(t => ((SreType)t).Type).ToArray()));
            }

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType? GenericTypeDefinition => Type.IsConstructedGenericType
                ? System.ResolveType(Type.GetGenericTypeDefinition())
                : null;

            public bool IsArray => Type.IsArray;

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType? ArrayElementType => IsArray ? System.ResolveType(Type.GetElementType()) : null;

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType MakeArrayType(int dimensions) => System.ResolveType(
                dimensions == 1 ? Type.MakeArrayType() : Type.MakeArrayType(dimensions));

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType? BaseType =>
                _baseType ??= Type.BaseType is { } baseType ? System.ResolveType(baseType) : null;

            public IXamlType? DeclaringType
                => _declaringType ??= Type.DeclaringType is { } declaringType ? System.ResolveType(declaringType) : null;

            public bool IsValueType => Type.IsValueType;
            public bool IsEnum => Type.IsEnum;

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType GetEnumUnderlyingType()
            {
                return System.ResolveType(Enum.GetUnderlyingType(Type));
            }

            public bool IsFunctionPointer
#if NET8_0_OR_GREATER
                => Type.IsFunctionPointer;
#else
                => false; // represented as IntPtr before .NET 8
#endif

            public override string ToString() => Type.ToString();
        }

        class SreCustomAttribute : IXamlCustomAttribute
        {
            private readonly CustomAttributeData _data;

            public SreCustomAttribute(SreTypeSystem system, CustomAttributeData data, IXamlType type)
            {
                Type = type;
                _data = data;
                object? ConvertAttributeValue(object? value)
                {
                    if (value is Type t)
                        return system.ResolveType(t);
                    if (value is CustomAttributeTypedArgument attr)
                        return attr.Value;
                    if (value is IEnumerable<CustomAttributeTypedArgument> array)
                        return array.Select(a => ConvertAttributeValue(a)).ToArray();
                    return value;
                }
                Parameters = data.ConstructorArguments.Select(p =>
                    ConvertAttributeValue(p.Value)).ToList();
                Properties = data.NamedArguments?.ToDictionary(x => x.MemberName,
                                 x => ConvertAttributeValue(x.TypedValue.Value)) ??
                             new Dictionary<string, object?>();
            }

            public bool Equals(IXamlCustomAttribute? other)
            {
                return other is SreCustomAttribute typedOther && typedOther._data.Equals(_data);
            }

            public IXamlType Type { get; }
            public List<object?> Parameters { get; }
            public Dictionary<string, object?> Properties { get; }
        }

        [DebuggerDisplay("{_parameterInfo}")]
        class SreXamlParameterInfo : IXamlParameterInfo
        {
            private readonly SreTypeSystem _system;
            private readonly ParameterInfo _parameterInfo;

            public SreXamlParameterInfo(SreTypeSystem system, ParameterInfo parameterInfo)
            {
                _system = system;
                _parameterInfo = parameterInfo;
            }

            public string Name => _parameterInfo.Name ?? string.Empty;
            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType ParameterType => _system.ResolveType(_parameterInfo.ParameterType);
            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _parameterInfo.GetCustomAttributesData()
                .Select(a => new SreCustomAttribute(_system, a, _system.ResolveType(a.AttributeType))).ToList();
        }
        
        [DebuggerDisplay("{_method}")]
        class SreMethodBase : SreMemberInfo
        {
            private readonly MethodBase _method;
            private IReadOnlyList<IXamlParameterInfo>? _parameters;
            private IXamlType? _declaringType;

            public SreMethodBase(SreTypeSystem system, MethodBase method) : base(system, method)
            {
                _method = method;
            }
            public bool IsPublic => _method.IsPublic;
            public bool IsPrivate => _method.IsPrivate;
            public bool IsFamily => _method.IsFamily;
            public bool IsStatic => _method.IsStatic;

            protected virtual IReadOnlyList<IXamlParameterInfo> SreParameters => _parameters ??=
                _method.GetParameters().Select(p => new SreXamlParameterInfo(System, p)).ToArray();

            public IReadOnlyList<IXamlType> Parameters =>
                SreParameters.Select(p => p.ParameterType).ToList();

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType DeclaringType
                => _declaringType ??= System.ResolveType(_method.DeclaringType!);

            public IXamlParameterInfo GetParameterInfo(int index) => SreParameters[index];
            
            public override string ToString() => _method.DeclaringType?.FullName + " " + _method;
        }

        [DebuggerDisplay("{Method}")]
        class SreMethod : SreMethodBase, IXamlMethod
        {
            public MethodInfo Method { get; }
            private readonly SreTypeSystem _system;
            private IReadOnlyList<IXamlType>? _genericParameters;
            private IReadOnlyList<IXamlType>? _genericArguments;

            public SreMethod(SreTypeSystem system, MethodInfo method) : base(system, method)
            {
                Method = method;
                _system = system;
            }

            public bool Equals(IXamlMethod? other)
                => other is SreMethod typedOther && Method == typedOther.Method;

            public override int GetHashCode()
                => Method.GetHashCode();

            [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments)
            {
                return new SreMethod(System, Method.MakeGenericMethod(typeArguments.Select(t => ((SreType)t).Type).ToArray()));
            }

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType ReturnType => _system.ResolveType(Method.ReturnType);


            public bool IsGenericMethod => Method.IsGenericMethod;

            public bool IsGenericMethodDefinition => Method.IsGenericMethodDefinition;

            public IReadOnlyList<IXamlType> GenericParameters => _genericParameters ??=
                Method.ContainsGenericParameters
                    ? Method.GetGenericArguments()
                            .Select(System.ResolveType)
                            .OfType<IXamlType>()
                            .ToArray()
                    : Array.Empty<IXamlType>();

            public IReadOnlyList<IXamlType> GenericArguments => _genericArguments ??=
                !Method.ContainsGenericParameters
                    ? Method.GetGenericArguments()
                            .Select(System.ResolveType)
                            .OfType<IXamlType>()
                            .ToArray()
                    : Array.Empty<IXamlType>();

            public bool ContainsGenericParameters => Method.ContainsGenericParameters;
        }

        class SreConstructor : SreMethodBase, IXamlConstructor
        {
            public ConstructorInfo Constuctor { get; }
            public SreConstructor(SreTypeSystem system, ConstructorInfo ctor) : base(system, ctor)
            {
                Constuctor = ctor;
            }

            public bool Equals(IXamlConstructor? other)
                => other is SreConstructor typedOther && Constuctor.Equals(typedOther.Constuctor);
        }

        class SreProperty : SreMemberInfo, IXamlProperty
        {
            private IReadOnlyList<IXamlType>? _parameters;
            private IXamlType? _declaringType;

            public PropertyInfo Member { get; }

            public SreProperty(SreTypeSystem system, PropertyInfo member) : base(system, member)
            {
                Member = member;
                Setter = member.SetMethod == null ? null : new SreMethod(system, member.SetMethod);
                Getter = member.GetMethod == null ? null : new SreMethod(system, member.GetMethod);
            }

            public bool Equals(IXamlProperty? other)
            {
                return other is SreProperty typedOther
                       && typedOther.Member.DeclaringType == Member.DeclaringType
                       && Member.Name == typedOther.Member.Name;
            }

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType PropertyType => System.ResolveType(Member.PropertyType);
            public IXamlMethod? Setter { get; }
            public IXamlMethod? Getter { get; }

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IReadOnlyList<IXamlType> IndexerParameters =>
                _parameters ??= Member.GetIndexParameters()
                    .Select(p => System.ResolveType(p.ParameterType)).ToList();

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType DeclaringType
                => _declaringType ??= System.ResolveType(Member.DeclaringType!);

            public override string? ToString() => Member.ToString();
        }

        class SreEvent : SreMemberInfo, IXamlEventInfo
        {
            private IXamlType? _declaringType;

            public EventInfo Event { get; }

            public SreEvent(SreTypeSystem system, EventInfo ev) : base(system, ev)
            {
                Event = ev;
                Add = ev.AddMethod is { } addMethod ? new SreMethod(system, addMethod) : null;
            }

            public IXamlMethod? Add { get; }

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType DeclaringType
                => _declaringType ??= System.ResolveType(Event.DeclaringType!);

            public bool Equals(IXamlEventInfo? other) => other is SreEvent typedOther && typedOther.Event.Equals(Event);

            public override string? ToString() => Event.ToString();
        }

        class SreField : SreMemberInfo, IXamlField
        {
            private IXamlType? _declaringType;

            public FieldInfo Field { get; }

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public SreField(SreTypeSystem system, FieldInfo field) : base(system, field)
            {
                Field = field;
                FieldType = system.ResolveType(field.FieldType);
            }

            public IXamlType FieldType { get; }
            public bool IsPublic => Field.IsPublic;
            public bool IsStatic => Field.IsStatic;
            public bool IsLiteral => Field.IsLiteral;

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
            public IXamlType DeclaringType
                => _declaringType ??= System.ResolveType(Field.DeclaringType!);

            public object GetLiteralValue()
            {
                if (!IsLiteral)
                    throw new InvalidOperationException($"{this} isn't a literal");
                return Field.GetValue(null)!;
            }

            public override string ToString() => Field.DeclaringType?.FullName + " " + Field.Name;
            public bool Equals(IXamlField? other) => other is SreField typedOther && typedOther.Field == Field;
            public override int GetHashCode() => Field.GetHashCode();
        }

        public IXamlILEmitter CreateCodeGen(MethodBuilder mb)
        {
            return new IlGen(this, mb.GetILGenerator());
        }

        public Type GetType(IXamlType t) => ((SreType)t).Type;
        public IXamlType GetType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) => ResolveType(t);

        public IXamlTypeBuilder<IXamlILEmitter> CreateTypeBuilder([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TypeBuilder builder) => new SreTypeBuilder(this, builder);

        class IlGen : IXamlILEmitter
        {
            private readonly ILGenerator _ilg;
            public IXamlTypeSystem TypeSystem { get; }

            public IlGen(SreTypeSystem system, ILGenerator ilg)
            {
                TypeSystem = system;
                _ilg = ilg;
                LocalsPool = new XamlLocalsPool(t => this.DefineLocal(t));
            }

            public IXamlILEmitter Emit(OpCode code)
            {
                _ilg.Emit(code);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, IXamlMethod method)
            {
                _ilg.Emit(code, ((SreMethod)method).Method);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, IXamlConstructor ctor)
            {
                _ilg.Emit(code, ((SreConstructor)ctor).Constuctor);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, string arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, int arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, long arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, sbyte arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, byte arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, float arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, double arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlLocal DefineLocal(IXamlType type)
            {
                return new SreLocal(_ilg.DeclareLocal(((SreType)type).Type));
            }

            public IXamlLabel DefineLabel()
            {
                return new SreLabel(_ilg.DefineLabel());
            }

            public IXamlILEmitter MarkLabel(IXamlLabel label)
            {
                _ilg.MarkLabel(((SreLabel)label).Label);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, IXamlLabel label)
            {
                _ilg.Emit(code, ((SreLabel)label).Label);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, IXamlLocal local)
            {
                _ilg.Emit(code, ((SreLocal)local).Local);
                return this;
            }


            public void InsertSequencePoint(IFileSource file, int line, int position)
            {
            }

            public XamlLocalsPool LocalsPool { get; }

            public IXamlILEmitter Emit(OpCode code, IXamlType type)
            {
                _ilg.Emit(code, ((SreType)type).Type);
                return this;
            }


            public IXamlILEmitter Emit(OpCode code, IXamlField field)
            {
                _ilg.Emit(code, ((SreField)field).Field);
                return this;
            }

            class SreLabel : IXamlLabel
            {
                public Label Label { get; }

                public SreLabel(Label label)
                {
                    Label = label;
                }
            }

            class SreLocal : IXamlILLocal
            {
                public LocalBuilder Local { get; }

                public int Index => Local.LocalIndex;

                public SreLocal(LocalBuilder local)
                {
                    Local = local;
                }
            }
        }

        class SreTypeBuilder : SreType, IXamlTypeBuilder<IXamlILEmitter>
        {
            private readonly SreTypeSystem _system;
            private readonly TypeBuilder _tb;

            public SreTypeBuilder(SreTypeSystem system, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TypeBuilder tb) : base(system, null, tb)
            {
                _system = system;
                _tb = tb;
            }

            public IXamlField DefineField(IXamlType type, string name, XamlVisibility visibility, bool isStatic)
            {
                var attrs = default(FieldAttributes);

                attrs |= visibility switch
                {
                    XamlVisibility.Public => FieldAttributes.Public,
                    XamlVisibility.Assembly => FieldAttributes.Assembly,
                    XamlVisibility.Private => FieldAttributes.Private,
                    _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
                };

                if (isStatic)
                    attrs |= FieldAttributes.Static;

                var f = _tb.DefineField(name, ((SreType)type).Type, attrs);
                return new SreField(_system, f);
            }

            public void AddInterfaceImplementation(IXamlType type)
            {
                _tb.AddInterfaceImplementation(((SreType)type).Type);
            }

            class SreMethodBuilder : SreMethod, IXamlMethodBuilder<IXamlILEmitter>
            {
                private readonly IReadOnlyList<IXamlParameterInfo> _parameters;
                public MethodBuilder MethodBuilder { get; }

                public SreMethodBuilder(SreTypeSystem system, MethodBuilder methodBuilder,
                    IReadOnlyList<IXamlType> parameters) : base(system, methodBuilder)
                {
                    MethodBuilder = methodBuilder;
                    Generator = new IlGen(system, methodBuilder.GetILGenerator());
                    _parameters = parameters.Select((p, i) => new AnonymousParameterInfo(p, i)).ToArray();
                }

                protected override IReadOnlyList<IXamlParameterInfo> SreParameters => _parameters;

                public IXamlILEmitter Generator { get; }

                public void EmitClosure(IEnumerable<IXamlType> fields)
                {
                    throw new NotImplementedException();
                }
            }

            public IXamlMethodBuilder<IXamlILEmitter> DefineMethod(IXamlType returnType, IEnumerable<IXamlType> args, string name,
                XamlVisibility visibility, bool isStatic,
                bool isInterfaceImpl, IXamlMethod? overrideMethod)
            {
                var attrs = default(MethodAttributes);

                attrs |= visibility switch
                {
                    XamlVisibility.Public => MethodAttributes.Public,
                    XamlVisibility.Assembly => MethodAttributes.Assembly,
                    XamlVisibility.Private => MethodAttributes.Private,
                    _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
                };

                if (isStatic)
                    attrs |= MethodAttributes.Static;

                if (isInterfaceImpl)
                    attrs |= MethodAttributes.NewSlot | MethodAttributes.Virtual;

                var ret = ((SreType) returnType).Type;
                var largs = args.ToList();
                var argTypes = largs.Cast<SreType>().Select(t => t.Type);
                var m = _tb.DefineMethod(name, attrs, ret, argTypes.ToArray());
                if (overrideMethod != null)
                    _tb.DefineMethodOverride(m, ((SreMethod)overrideMethod).Method);

                return new SreMethodBuilder(_system, m, largs);
            }

            public IXamlProperty DefineProperty(IXamlType propertyType, string name, IXamlMethod? setter, IXamlMethod? getter)
            {
                var p = _tb.DefineProperty(name, PropertyAttributes.None, ((SreType) propertyType).Type, []);
                if (setter != null)
                    p.SetSetMethod(((SreMethodBuilder)setter).MethodBuilder);
                if (getter != null)
                    p.SetGetMethod(((SreMethodBuilder)getter).MethodBuilder);
                return new SreProperty(_system, p);
            }

            class SreConstructorBuilder : SreConstructor, IXamlConstructorBuilder<IXamlILEmitter>
            {
                public SreConstructorBuilder(SreTypeSystem system, ConstructorBuilder ctor) : base(system, ctor)
                {
                    Generator = new IlGen(system, ctor.GetILGenerator());
                }

                public IXamlILEmitter Generator { get; }
            }


            public IXamlConstructorBuilder<IXamlILEmitter> DefineConstructor(bool isStatic, params IXamlType[] args)
            {
                var attrs = MethodAttributes.Public;
                if (isStatic)
                    attrs |= MethodAttributes.Static;
                var ctor = _tb.DefineConstructor(attrs,
                    CallingConventions.Standard,
                    args.Cast<SreType>().Select(t => t.Type).ToArray());
                return new SreConstructorBuilder(_system, ctor);
            }

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.GeneratedTypes)]
            public IXamlType CreateType() => new SreType(_system, null, _tb.CreateTypeInfo()!);

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.GeneratedTypes)]
            public IXamlTypeBuilder<IXamlILEmitter> DefineSubType(IXamlType baseType, string name, XamlVisibility visibility)
            {
                var attrs = TypeAttributes.Class;

                attrs |= visibility switch
                {
                    XamlVisibility.Public => TypeAttributes.NestedPublic,
                    XamlVisibility.Assembly => TypeAttributes.NestedAssembly,
                    XamlVisibility.Private => TypeAttributes.NestedPrivate,
                    _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
                };

                var builder = _tb.DefineNestedType(name, attrs,
                    ((SreType)baseType).Type);

                return new SreTypeBuilder(_system, builder);
            }

            [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.GeneratedTypes)]
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.GeneratedTypes)]
            [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = TrimmingMessages.GeneratedTypes)]
            public IXamlTypeBuilder<IXamlILEmitter> DefineDelegateSubType(string name, XamlVisibility visibility,
                IXamlType returnType, IEnumerable<IXamlType> parameterTypes)
            {
                var attrs = TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoLayout;

                attrs |= visibility switch
                {
                    XamlVisibility.Public => TypeAttributes.NestedPublic,
                    XamlVisibility.Assembly => TypeAttributes.NestedAssembly,
                    XamlVisibility.Private => TypeAttributes.NestedPrivate,
                    _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
                };

                var builder = _tb.DefineNestedType(name, attrs, typeof(MulticastDelegate));

                builder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) })
                    .SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.Runtime);

                builder.DefineMethod("Invoke",
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    ((SreType)returnType).Type,
                    parameterTypes.Select(p => ((SreType)p).Type).ToArray())
                    .SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.Runtime);

                return new SreTypeBuilder(_system, builder);
            }

            public void DefineGenericParameters(IReadOnlyList<KeyValuePair<string, XamlGenericParameterConstraint>> args)
            {
                var builders = _tb.DefineGenericParameters(args.Select(x => x.Key).ToArray());
                for (var c = 0; c < args.Count; c++)
                {
                    if (args[c].Value.IsClass)
                        builders[c].SetGenericParameterAttributes(GenericParameterAttributes.ReferenceTypeConstraint);
                }
            }
        }

        public IXamlAssembly? GetAssembly(Assembly asm) => ResolveAssembly(asm);
    }
}
#endif
