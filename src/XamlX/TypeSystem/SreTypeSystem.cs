using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
#if !XAMLIL_NO_SRE
namespace XamlX.TypeSystem
{
#if !XAMLIL_INTERNAL
    public
#endif
    class SreTypeSystem : IXamlXTypeSystem
    {
        private List<IXamlXAssembly> _assemblies = new List<IXamlXAssembly>();
        public IReadOnlyList<IXamlXAssembly> Assemblies => _assemblies;
        
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
        
        public IXamlXAssembly FindAssembly(string name)
        {
            return Assemblies.FirstOrDefault(a => a.Name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        SreAssembly ResolveAssembly(Assembly asm)
        {
            if (asm.IsDynamic)
                return null;
            foreach(var a in Assemblies)
                if (((SreAssembly) a).Assembly == asm)
                    return (SreAssembly)a;
            var n = new SreAssembly(this, asm);
            _assemblies.Add(n);
            n.Init();
            return n;
        }

        SreType ResolveType(Type t)
        {
            if (_typeDic.TryGetValue(t, out var rv))
                return rv;
            _typeDic[t] = rv = new SreType(this, ResolveAssembly(t.Assembly), t);
            return rv;
        }

        public IXamlXType FindType(string name, string asm)
        {
            if (asm != null)
                name += ", " + asm;
            var found = Type.GetType(name);
            if (found == null)
                return null;
            return ResolveType(found);
        }
        public IXamlXType FindType(string name)
        {
            foreach (var asm in Assemblies)
            {
                var t = asm.FindType(name);
                if (t != null)
                    return t;
            }

            return null;
        }

        class SreAssembly : IXamlXAssembly
        {
            private readonly SreTypeSystem _system;
            private IReadOnlyList<IXamlXCustomAttribute> _customAttributes;
            public Assembly Assembly { get; }

            public SreAssembly(SreTypeSystem system, Assembly asm)
            {
                _system = system;
                Assembly = asm;
            }


            public bool Equals(IXamlXAssembly other) => Assembly == ((SreAssembly) other)?.Assembly;

            public string Name => Assembly.GetName().Name;
            
            public IReadOnlyList<IXamlXType> Types { get; private set; }
            private Dictionary<string, SreType> _typeDic = new Dictionary<string, SreType>();

            public IReadOnlyList<IXamlXCustomAttribute> CustomAttributes
                => _customAttributes ??
                   (_customAttributes = Assembly.GetCustomAttributesData().Select(a => new SreCustomAttribute(
                       _system, a, _system.ResolveType(a.AttributeType))).ToList());

            public IXamlXType FindType(string fullName)
            {
                _typeDic.TryGetValue(fullName, out var rv);
                return rv;
            }

            public void Init()
            {
                var types = Assembly.GetExportedTypes().Select(t => _system.ResolveType(t)).ToList();
                Types = types;
                _typeDic = types.ToDictionary(t => t.Type.FullName);
            }
        }

        class SreMemberInfo
        {
            protected readonly SreTypeSystem System;
            private readonly MemberInfo _member;
            private  IReadOnlyList<IXamlXCustomAttribute> _customAttributes;
            public string Name => _member.Name;
            public SreMemberInfo(SreTypeSystem system, MemberInfo member)
            {
                System = system;
                _member = member;
            }

            public IReadOnlyList<IXamlXCustomAttribute> CustomAttributes
                => _customAttributes ??
                   (_customAttributes = _member.GetCustomAttributesData().Select(a => new SreCustomAttribute(System,
                       a, System.ResolveType(a.AttributeType))).ToList());
        }
        
        [DebuggerDisplay("{" + nameof(Type) + "}")]
        class SreType : SreMemberInfo, IXamlXType
        {
            private IReadOnlyList<IXamlXProperty> _properties;
            private IReadOnlyList<IXamlXField> _fields;
            private IReadOnlyList<IXamlXMethod> _methods;
            private IReadOnlyList<IXamlXConstructor> _constructors;
            private IReadOnlyList<IXamlXType> _genericArguments;
            private IReadOnlyList<IXamlXType> _genericParameters;
            private IReadOnlyList<IXamlXType> _interfaces;
            private IReadOnlyList<IXamlXEventInfo> _events;
            public Type Type { get; }

            public SreType(SreTypeSystem system, SreAssembly asm, Type type): base(system, type)
            {
                Assembly = asm;
                Type = type;
            }

            public bool Equals(IXamlXType other) => Type == (other as SreType)?.Type;
            public override int GetHashCode() => Type.GetHashCode();
            public object Id => Type;

            public string FullName => Type.FullName;
            public string Namespace => Type.Namespace;
            public IXamlXAssembly Assembly { get; }

            public IReadOnlyList<IXamlXProperty> Properties =>
                _properties ?? (_properties = Type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Select(p => new SreProperty(System, p)).ToList());

            public IReadOnlyList<IXamlXField> Fields =>
                _fields ?? (_fields = Type.GetFields(BindingFlags.Public | BindingFlags.Static
                                                                         | BindingFlags.Instance |
                                                                         BindingFlags.NonPublic
                                                                         | BindingFlags.DeclaredOnly)
                    .Select(f => new SreField(System, f)).ToList());

            public IReadOnlyList<IXamlXMethod> Methods =>
                _methods ?? (_methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Static |
                                                        BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(m => new SreMethod(System, m)).ToList());

            public IReadOnlyList<IXamlXConstructor> Constructors =>
                _constructors ?? (_constructors = Type.GetConstructors(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(c => new SreConstructor(System, c)).ToList());

            public IReadOnlyList<IXamlXType> Interfaces =>
                _interfaces ?? (_interfaces = Type.GetInterfaces().Select(System.ResolveType).ToList());

            public IReadOnlyList<IXamlXEventInfo> Events =>
                _events ?? (_events = Type
                    .GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                    .Select(e => new SreEvent(System, e)).ToList());

            public bool IsInterface => Type.IsInterface;

            public IReadOnlyList<IXamlXType> GenericArguments
            {
                get
                {
                    if (_genericArguments != null)
                        return _genericArguments;
                    if (GenericTypeDefinition == null)
                        return _genericArguments = new IXamlXType[0];
                    return _genericArguments = Type.GetGenericArguments().Select(System.ResolveType).ToList();
                }
            }

            public IReadOnlyList<IXamlXType> GenericParameters =>
                _genericParameters ?? (_genericParameters =
                    Type.GetTypeInfo().GenericTypeParameters.Select(System.ResolveType).ToList());

            public bool IsAssignableFrom(IXamlXType type)
            {
                if (type == XamlXPseudoType.Null)
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

            public IXamlXType MakeGenericType(IReadOnlyList<IXamlXType> typeArguments)
            {
                return System.ResolveType(
                    Type.MakeGenericType(typeArguments.Select(t => ((SreType) t).Type).ToArray()));
            }

            public IXamlXType GenericTypeDefinition => Type.IsConstructedGenericType
                ? System.ResolveType(Type.GetGenericTypeDefinition())
                : null;

            public bool IsArray => Type.IsArray;
            public IXamlXType ArrayElementType => IsArray ? System.ResolveType(Type.GetElementType()) : null;

            public IXamlXType MakeArrayType(int dimensions) => System.ResolveType(
                dimensions == 1 ? Type.MakeArrayType() : Type.MakeArrayType(dimensions));

            public IXamlXType BaseType => Type.BaseType == null ? null : System.ResolveType(Type.BaseType);
            public bool IsValueType => Type.IsValueType;
            public bool IsEnum => Type.IsEnum;
            public IXamlXType GetEnumUnderlyingType()
            {
                return System.ResolveType(Enum.GetUnderlyingType(Type));
            }
            public override string ToString() => Type.ToString();
        }

        class SreCustomAttribute : IXamlXCustomAttribute
        {
            private readonly CustomAttributeData _data;

            public SreCustomAttribute(SreTypeSystem system, CustomAttributeData data, IXamlXType type)
            {
                Type = type;
                _data = data;
                Parameters = data.ConstructorArguments.Select(p =>
                    p.Value is Type t ? system.ResolveType(t) : p.Value
                ).ToList();
                Properties = data.NamedArguments?.ToDictionary(x => x.MemberName, x => x.TypedValue.Value) ??
                             new Dictionary<string, object>();
            }
            
            public bool Equals(IXamlXCustomAttribute other)
            {
                return ((SreCustomAttribute) other)?._data.Equals(_data) == true;
            }

            public IXamlXType Type { get; }
            public List<object> Parameters { get; }
            public Dictionary<string, object> Properties { get; }
        }

        [DebuggerDisplay("{_method}")]
        class SreMethodBase : SreMemberInfo
        {
            private readonly MethodBase _method;

            private IReadOnlyList<IXamlXType> _parameters;
            public SreMethodBase(SreTypeSystem system, MethodBase method) : base(system, method)
            {
                _method = method;
            }
            public bool IsPublic => _method.IsPublic;
            public bool IsStatic => _method.IsStatic;
            public IReadOnlyList<IXamlXType> Parameters =>
                _parameters ?? (_parameters = _method.GetParameters()
                    .Select(p => System.ResolveType(p.ParameterType)).ToList());

            public override string ToString() => _method.DeclaringType?.FullName + " " + _method;
        }
        
        [DebuggerDisplay("{Method}")]
        class SreMethod : SreMethodBase, IXamlXMethod
        {
            public MethodInfo Method { get; }
            private readonly SreTypeSystem _system;

            public SreMethod(SreTypeSystem system, MethodInfo method) : base(system, method)
            {
                Method = method;
                _system = system;
            }

            public bool Equals(IXamlXMethod other) => ((SreMethod) other)?.Method.Equals(Method) == true;
            public IXamlXType ReturnType => _system.ResolveType(Method.ReturnType);
            public IXamlXType DeclaringType => _system.ResolveType(Method.DeclaringType);
        }

        class SreConstructor : SreMethodBase, IXamlXConstructor
        {
            public ConstructorInfo Constuctor { get; }
            public SreConstructor(SreTypeSystem system, ConstructorInfo ctor) : base(system, ctor)
            {
                Constuctor = ctor;
            }

            public bool Equals(IXamlXConstructor other) 
                => ((SreConstructor) other)?.Constuctor.Equals(Constuctor) == true;
        }

        class SreProperty : SreMemberInfo, IXamlXProperty
        {
            public PropertyInfo Member { get; }

            public SreProperty(SreTypeSystem system, PropertyInfo member) : base(system, member)
            {
                Member = member;
                Setter = member.SetMethod == null ? null : new SreMethod(system, member.SetMethod);
                Getter = member.GetMethod == null ? null : new SreMethod(system, member.GetMethod);
            }

            public bool Equals(IXamlXProperty other)
            {
                var otherProp =((SreProperty) other)?.Member;
                if (otherProp == null)
                    return false;
                return otherProp?.DeclaringType?.Equals(Member.DeclaringType) == true
                       && Member.Name == otherProp.Name;
            }

            public IXamlXType PropertyType => System.ResolveType(Member.PropertyType);
            public IXamlXMethod Setter { get; }
            public IXamlXMethod Getter { get; }

            public override string ToString() => Member.ToString();
        }

        class SreEvent : SreMemberInfo, IXamlXEventInfo
        {
            public EventInfo Event { get; }
            public SreEvent(SreTypeSystem system, EventInfo ev) : base(system, ev)
            {
                Event = ev;
                Add = new SreMethod(system, ev.AddMethod);
            }
            public IXamlXMethod Add { get; }
            public bool Equals(IXamlXEventInfo other) => (other as SreEvent)?.Event.Equals(Event) == true;
            public override string ToString() => Event.ToString();
        }
        
        class SreField : SreMemberInfo, IXamlXField
        {
            public FieldInfo Field { get; }

            public SreField(SreTypeSystem system, FieldInfo field) : base(system, field)
            {
                Field = field;
                FieldType = system.ResolveType(field.FieldType);
            }

            public IXamlXType FieldType { get; }
            public bool IsPublic => Field.IsPublic;
            public bool IsStatic => Field.IsStatic;
            public bool IsLiteral => Field.IsLiteral;
            public object GetLiteralValue()
            {
                if (!IsLiteral)
                    throw new InvalidOperationException();
                return Field.GetValue(null);
            }

            public override string ToString() => Field.DeclaringType?.FullName + " " + Field.Name;
            public bool Equals(IXamlXField other) => ((SreField) other)?.Field.Equals(Field) == true;
        }

        public IXamlXEmitter CreateCodeGen(MethodBuilder mb)
        {
            return new IlGen(this, mb.GetILGenerator());
        }

        public Type GetType(IXamlXType t) => ((SreType) t).Type;
        public IXamlXType GetType(Type t) => ResolveType(t);

        public IXamlXTypeBuilder CreateTypeBuilder(TypeBuilder builder) => new SreTypeBuilder(this, builder);

        class IlGen : IXamlXEmitter
        {
            private readonly ILGenerator _ilg;
            public IXamlXTypeSystem TypeSystem { get; }

            public IlGen(SreTypeSystem system, ILGenerator ilg)
            {
                TypeSystem = system;
                _ilg = ilg;
                LocalsPool = new XamlXLocalsPool(this);
            }

            public IXamlXEmitter Emit(OpCode code)
            {
                _ilg.Emit(code);
                return this;
            }

            public IXamlXEmitter Emit(OpCode code, IXamlXMethod method)
            {
                _ilg.Emit(code, ((SreMethod) method).Method);
                return this;
            }

            public IXamlXEmitter Emit(OpCode code, IXamlXConstructor ctor)
            {
                _ilg.Emit(code, ((SreConstructor) ctor).Constuctor);
                return this;
            }

            public IXamlXEmitter Emit(OpCode code, string arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlXEmitter Emit(OpCode code, int arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlXEmitter Emit(OpCode code, long arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }
            
            public IXamlXEmitter Emit(OpCode code, float arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }
            
            public IXamlXEmitter Emit(OpCode code, double arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlXLocal DefineLocal(IXamlXType type)
            {
                return new SreLocal(_ilg.DeclareLocal(((SreType) type).Type));
            }

            public IXamlXLabel DefineLabel()
            {
                return new SreLabel(_ilg.DefineLabel());
            }

            public IXamlXEmitter MarkLabel(IXamlXLabel label)
            {
                _ilg.MarkLabel(((SreLabel) label).Label);
                return this;
            }

            public IXamlXEmitter Emit(OpCode code, IXamlXLabel label)
            {
                _ilg.Emit(code, ((SreLabel)label).Label);
                return this;
            }

            public IXamlXEmitter Emit(OpCode code, IXamlXLocal local)
            {
                _ilg.Emit(code, ((SreLocal) local).Local);
                return this;
            }


            public void InsertSequencePoint(IFileSource file, int line, int position)
            {
            }

            public XamlXLocalsPool LocalsPool { get; }

            public IXamlXEmitter Emit(OpCode code, IXamlXType type)
            {
                _ilg.Emit(code, ((SreType) type).Type);
                return this;
            }
            
            
            public IXamlXEmitter Emit(OpCode code, IXamlXField field)
            {
                _ilg.Emit(code, ((SreField) field).Field);
                return this;
            }

            class SreLabel : IXamlXLabel
            {
                public Label Label { get; }

                public SreLabel(Label label)
                {
                    Label = label;
                }
            }
            
            class SreLocal : IXamlXLocal
            {
                public LocalBuilder Local { get; }

                public SreLocal(LocalBuilder local)
                {
                    Local = local;
                }
            }
        }

        class SreTypeBuilder : SreType, IXamlXTypeBuilder
        {
            private readonly SreTypeSystem _system;
            private readonly TypeBuilder _tb;

            public SreTypeBuilder(SreTypeSystem system, TypeBuilder tb) : base(system,null, tb)
            {
                _system = system;
                _tb = tb;
            }
            
            public IXamlXField DefineField(IXamlXType type, string name, bool isPublic, bool isStatic)
            {
                var f = _tb.DefineField(name, ((SreType) type).Type,
                    (isPublic ? FieldAttributes.Public : FieldAttributes.Private)
                    | (isStatic ? FieldAttributes.Static : default(FieldAttributes)));
                return new SreField(_system, f);
            }

            public void AddInterfaceImplementation(IXamlXType type)
            {
                _tb.AddInterfaceImplementation(((SreType)type).Type);
            }

            class SreMethodBuilder : SreMethod, IXamlXMethodBuilder
            {
                public MethodBuilder MethodBuilder { get; }

                public SreMethodBuilder(SreTypeSystem system, MethodBuilder methodBuilder) : base(system, methodBuilder)
                {
                    MethodBuilder = methodBuilder;
                    Generator = new IlGen(system, methodBuilder.GetILGenerator());
                }

                public IXamlXEmitter Generator { get; }

                public void EmitClosure(IEnumerable<IXamlXType> fields)
                {
                    throw new NotImplementedException();
                }
            }
            
            public IXamlXMethodBuilder DefineMethod(IXamlXType returnType, IEnumerable<IXamlXType> args, string name,
                bool isPublic, bool isStatic,
                bool isInterfaceImpl, IXamlXMethod overrideMethod)
            {
                var ret = ((SreType) returnType).Type;
                var argTypes = args?.Cast<SreType>().Select(t => t.Type) ?? Type.EmptyTypes;
                var m = _tb.DefineMethod(name, 
                    (isPublic ? MethodAttributes.Public : MethodAttributes.Private)
                    |(isStatic ? MethodAttributes.Static : default(MethodAttributes))
                    |(isInterfaceImpl ? MethodAttributes.Virtual|MethodAttributes.NewSlot : default(MethodAttributes))
                    , ret, argTypes.ToArray());
                if (overrideMethod != null)
                    _tb.DefineMethodOverride(m, ((SreMethod) overrideMethod).Method);
               
                return new SreMethodBuilder(_system, m);
            }

            public IXamlXProperty DefineProperty(IXamlXType propertyType, string name, IXamlXMethod setter, IXamlXMethod getter)
            {
                var p = _tb.DefineProperty(name, PropertyAttributes.None, ((SreType) propertyType).Type, new Type[0]);
                if (setter != null)
                    p.SetSetMethod(((SreMethodBuilder) setter).MethodBuilder);
                if (getter != null)
                    p.SetGetMethod(((SreMethodBuilder) getter).MethodBuilder);
                return new SreProperty(_system, p);
            }

            class SreConstructorBuilder : SreConstructor, IXamlXConstructorBuilder
            {
                public SreConstructorBuilder(SreTypeSystem system, ConstructorBuilder ctor) : base(system, ctor)
                {
                    Generator = new IlGen(system, ctor.GetILGenerator());
                }

                public IXamlXEmitter Generator { get; }
            }

            
            public IXamlXConstructorBuilder DefineConstructor(bool isStatic, params IXamlXType[] args)
            {
                var attrs = MethodAttributes.Public;
                if (isStatic)
                    attrs |= MethodAttributes.Static;
                var ctor = _tb.DefineConstructor(attrs,
                    CallingConventions.Standard,
                    args.Cast<SreType>().Select(t => t.Type).ToArray());
                return new SreConstructorBuilder(_system, ctor);
            }
            
            public IXamlXType CreateType() => new SreType(_system, null, _tb.CreateTypeInfo());
            public IXamlXTypeBuilder DefineSubType(IXamlXType baseType, string name, bool isPublic)
            {
                var attrs = TypeAttributes.Class;
                if (isPublic)
                    attrs |= TypeAttributes.NestedPublic;
                else
                    attrs |= TypeAttributes.NestedPrivate;
                
                var builder  = _tb.DefineNestedType(name, attrs,
                    ((SreType) baseType).Type);
                
                return new SreTypeBuilder(_system, builder);
            }

            public void DefineGenericParameters(IReadOnlyList<KeyValuePair<string, XamlXGenericParameterConstraint>> args)
            {
                var builders = _tb.DefineGenericParameters(args.Select(x=>x.Key).ToArray());
                for (var c = 0; c < args.Count; c++)
                {
                    if (args[c].Value.IsClass)
                        builders[c].SetGenericParameterAttributes(GenericParameterAttributes.ReferenceTypeConstraint);
                }
            }
        }

        public IXamlXAssembly GetAssembly(Assembly asm) => ResolveAssembly(asm);
    }
}
#endif
