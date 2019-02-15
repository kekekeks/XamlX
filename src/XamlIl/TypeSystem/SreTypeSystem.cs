using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace XamlIl.TypeSystem
{
    public class SreTypeSystem : IXamlIlTypeSystem
    {
        private List<IXamlIlAssembly> _assemblies = new List<IXamlIlAssembly>();
        public IReadOnlyList<IXamlIlAssembly> Assemblies => _assemblies;
        
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
        
        public IXamlIlAssembly FindAssembly(string name)
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

        public IXamlIlType FindType(string name, string asm)
        {
            if (asm != null)
                name += ", " + asm;
            var found = Type.GetType(name);
            if (found == null)
                return null;
            return ResolveType(found);
        }
        public IXamlIlType FindType(string name)
        {
            foreach (var asm in Assemblies)
            {
                var t = asm.FindType(name);
                if (t != null)
                    return t;
            }

            return null;
        }

        class SreAssembly : IXamlIlAssembly
        {
            private readonly SreTypeSystem _system;
            private IReadOnlyList<IXamlIlCustomAttribute> _customAttributes;
            public Assembly Assembly { get; }

            public SreAssembly(SreTypeSystem system, Assembly asm)
            {
                _system = system;
                Assembly = asm;
            }


            public bool Equals(IXamlIlAssembly other) => Assembly == ((SreAssembly) other)?.Assembly;

            public string Name => Assembly.GetName().Name;
            
            public IReadOnlyList<IXamlIlType> Types { get; private set; }
            private Dictionary<string, SreType> _typeDic = new Dictionary<string, SreType>();

            public IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes
                => _customAttributes ??
                   (_customAttributes = Assembly.GetCustomAttributesData().Select(a => new SreCustomAttribute(
                       _system, a, _system.ResolveType(a.AttributeType))).ToList());

            public IXamlIlType FindType(string fullName)
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
            private  IReadOnlyList<IXamlIlCustomAttribute> _customAttributes;
            public string Name => _member.Name;
            public SreMemberInfo(SreTypeSystem system, MemberInfo member)
            {
                System = system;
                _member = member;
            }

            public IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes
                => _customAttributes ??
                   (_customAttributes = _member.GetCustomAttributesData().Select(a => new SreCustomAttribute(System,
                       a, System.ResolveType(a.AttributeType))).ToList());
        }
        
        [DebuggerDisplay("{" + nameof(Type) + "}")]
        class SreType : SreMemberInfo, IXamlIlType
        {
            private IReadOnlyList<IXamlIlProperty> _properties;
            private IReadOnlyList<IXamlIlField> _fields;
            private IReadOnlyList<IXamlIlMethod> _methods;
            private IReadOnlyList<IXamlIlConstructor> _constructors;
            private IReadOnlyList<IXamlIlType> _genericArguments;
            private IReadOnlyList<IXamlIlType> _interfaces;
            public Type Type { get; }

            public SreType(SreTypeSystem system, SreAssembly asm, Type type): base(system, type)
            {
                Assembly = asm;
                Type = type;
            }

            public bool Equals(IXamlIlType other) => Type == (other as SreType)?.Type;

            public object Id => Type;

            public string FullName => Type.FullName;
            public string Namespace => Type.Namespace;
            public IXamlIlAssembly Assembly { get; }

            public IReadOnlyList<IXamlIlProperty> Properties =>
                _properties ?? (_properties = Type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Select(p => new SreProperty(System, p)).ToList());

            public IReadOnlyList<IXamlIlField> Fields =>
                _fields ?? (_fields = Type.GetFields(BindingFlags.Public | BindingFlags.Static
                                                                         | BindingFlags.Instance |
                                                                         BindingFlags.NonPublic
                                                                         | BindingFlags.DeclaredOnly)
                    .Select(f => new SreField(System, f)).ToList());

            public IReadOnlyList<IXamlIlMethod> Methods =>
                _methods ?? (_methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Static |
                                                        BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(m => new SreMethod(System, m)).ToList());

            public IReadOnlyList<IXamlIlConstructor> Constructors =>
                _constructors ?? (_constructors = Type.GetConstructors(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(c => new SreConstructor(System, c)).ToList());

            public IReadOnlyList<IXamlIlType> Interfaces =>
                _interfaces ?? (_interfaces = Type.GetInterfaces().Select(System.ResolveType).ToList());

            public bool IsInterface => Type.IsInterface;

            public IReadOnlyList<IXamlIlType> GenericArguments
            {
                get
                {
                    if (_genericArguments != null)
                        return _genericArguments;
                    if (GenericTypeDefinition == null)
                        return _genericArguments = new IXamlIlType[0];
                    return _genericArguments = Type.GetGenericArguments().Select(System.ResolveType).ToList();
                }
            }

            public bool IsAssignableFrom(IXamlIlType type)
            {
                if (type == XamlIlPseudoType.Null)
                {
                    if (!Type.IsValueType)
                        return true;
                    if (Type.IsConstructedGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return true;
                    return false;
                }
                return Type.IsAssignableFrom(((SreType) type).Type);
            }

            public IXamlIlType MakeGenericType(IReadOnlyList<IXamlIlType> typeArguments)
            {
                return System.ResolveType(
                    Type.MakeGenericType(typeArguments.Select(t => ((SreType) t).Type).ToArray()));
            }

            public IXamlIlType GenericTypeDefinition => Type.IsConstructedGenericType
                ? System.ResolveType(Type.GetGenericTypeDefinition())
                : null;

            public IXamlIlType BaseType => Type.BaseType == null ? null : System.ResolveType(Type.BaseType);
            public bool IsValueType => Type.IsValueType;
            public bool IsEnum => Type.IsEnum;
            public IXamlIlType GetEnumUnderlyingType()
            {
                return System.ResolveType(Enum.GetUnderlyingType(Type));
            }
        }

        class SreCustomAttribute : IXamlIlCustomAttribute
        {
            private readonly CustomAttributeData _data;

            public SreCustomAttribute(SreTypeSystem system, CustomAttributeData data, IXamlIlType type)
            {
                Type = type;
                _data = data;
                Parameters = data.ConstructorArguments.Select(p =>
                    p.Value is Type t ? system.ResolveType(t) : p.Value
                ).ToList();
                Properties = data.NamedArguments?.ToDictionary(x => x.MemberName, x => x.TypedValue.Value) ??
                             new Dictionary<string, object>();
            }
            
            public bool Equals(IXamlIlCustomAttribute other)
            {
                return ((SreCustomAttribute) other)?._data.Equals(_data) == true;
            }

            public IXamlIlType Type { get; }
            public List<object> Parameters { get; }
            public Dictionary<string, object> Properties { get; }
        }

        [DebuggerDisplay("{_method}")]
        class SreMethodBase : SreMemberInfo
        {
            private readonly MethodBase _method;

            private IReadOnlyList<IXamlIlType> _parameters;
            public SreMethodBase(SreTypeSystem system, MethodBase method) : base(system, method)
            {
                _method = method;
            }
            public bool IsPublic => _method.IsPublic;
            public bool IsStatic => _method.IsStatic;
            public IReadOnlyList<IXamlIlType> Parameters =>
                _parameters ?? (_parameters = _method.GetParameters()
                    .Select(p => System.ResolveType(p.ParameterType)).ToList());
        }
        
        [DebuggerDisplay("{Method}")]
        class SreMethod : SreMethodBase, IXamlIlMethod
        {
            public MethodInfo Method { get; }
            private readonly SreTypeSystem _system;

            public SreMethod(SreTypeSystem system, MethodInfo method) : base(system, method)
            {
                Method = method;
                _system = system;
            }

            public bool Equals(IXamlIlMethod other) => ((SreMethod) other)?.Method.Equals(Method) == true;
            public IXamlIlType ReturnType => _system.ResolveType(Method.ReturnType);
            public IXamlIlType DeclaringType => _system.ResolveType(Method.DeclaringType);
        }

        class SreConstructor : SreMethodBase, IXamlIlConstructor
        {
            public ConstructorInfo Constuctor { get; }
            public SreConstructor(SreTypeSystem system, ConstructorInfo ctor) : base(system, ctor)
            {
                Constuctor = ctor;
            }

            public bool Equals(IXamlIlConstructor other) 
                => ((SreConstructor) other)?.Constuctor.Equals(Constuctor) == true;
        }

        class SreProperty : SreMemberInfo, IXamlIlProperty
        {
            public PropertyInfo Member { get; }

            public SreProperty(SreTypeSystem system, PropertyInfo member) : base(system, member)
            {
                Member = member;
                Setter = member.SetMethod == null ? null : new SreMethod(system, member.SetMethod);
                Getter = member.GetMethod == null ? null : new SreMethod(system, member.GetMethod);
            }

            public bool Equals(IXamlIlProperty other)
            {
                var otherProp =((SreProperty) other)?.Member;
                if (otherProp == null)
                    return false;
                return otherProp?.DeclaringType?.Equals(Member.DeclaringType) == true
                       && Member.Name == otherProp.Name;
            }

            public IXamlIlType PropertyType => System.ResolveType(Member.PropertyType);
            public IXamlIlMethod Setter { get; }
            public IXamlIlMethod Getter { get; }
        }
        
        class SreField : SreMemberInfo, IXamlIlField
        {
            public FieldInfo Field { get; }

            private IReadOnlyList<IXamlIlType> _parameters;
            public SreField(SreTypeSystem system, FieldInfo field) : base(system, field)
            {
                Field = field;
                FieldType = system.ResolveType(field.FieldType);
            }

            public IXamlIlType FieldType { get; }
            public bool IsPublic => Field.IsPublic;
            public bool IsStatic => Field.IsStatic;
            public bool IsLiteral => Field.IsLiteral;
            public object GetLiteralValue()
            {
                if (!IsLiteral)
                    throw new InvalidOperationException();
                return Field.GetValue(null);
            }

            public bool Equals(IXamlIlField other) => ((SreField) other)?.Field.Equals(Field) == true;
        }

        public IXamlIlEmitter CreateCodeGen(MethodBuilder mb)
        {
            return new IlGen(this, mb.GetILGenerator());
        }

        public Type GetType(IXamlIlType t) => ((SreType) t).Type;
        public IXamlIlType GetType(Type t) => ResolveType(t);

        public IXamlIlTypeBuilder CreateTypeBuilder(TypeBuilder builder) => new SreTypeBuilder(this, builder);

        class IlGen : IXamlIlEmitter
        {
            private readonly ILGenerator _ilg;
            public IXamlIlTypeSystem TypeSystem { get; }

            public IlGen(SreTypeSystem system, ILGenerator ilg)
            {
                TypeSystem = system;
                _ilg = ilg;
            }

            public IXamlIlEmitter Emit(OpCode code)
            {
                _ilg.Emit(code);
                return this;
            }

            public IXamlIlEmitter Emit(OpCode code, IXamlIlMethod method)
            {
                _ilg.Emit(code, ((SreMethod) method).Method);
                return this;
            }

            public IXamlIlEmitter Emit(OpCode code, IXamlIlConstructor ctor)
            {
                _ilg.Emit(code, ((SreConstructor) ctor).Constuctor);
                return this;
            }

            public IXamlIlEmitter Emit(OpCode code, string arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlIlEmitter Emit(OpCode code, int arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlIlEmitter Emit(OpCode code, long arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }
            
            public IXamlIlEmitter Emit(OpCode code, float arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }
            
            public IXamlIlEmitter Emit(OpCode code, double arg)
            {
                _ilg.Emit(code, arg);
                return this;
            }

            public IXamlIlLocal DefineLocal(IXamlIlType type)
            {
                return new SreLocal(_ilg.DeclareLocal(((SreType) type).Type));
            }

            public IXamlIlLabel DefineLabel()
            {
                return new SreLabel(_ilg.DefineLabel());
            }

            public IXamlIlEmitter MarkLabel(IXamlIlLabel label)
            {
                _ilg.MarkLabel(((SreLabel) label).Label);
                return this;
            }

            public IXamlIlEmitter Emit(OpCode code, IXamlIlLabel label)
            {
                _ilg.Emit(code, ((SreLabel)label).Label);
                return this;
            }

            public IXamlIlEmitter Emit(OpCode code, IXamlIlLocal local)
            {
                _ilg.Emit(code, ((SreLocal) local).Local);
                return this;
            }

            public IXamlIlEmitter Emit(OpCode code, IXamlIlType type)
            {
                _ilg.Emit(code, ((SreType) type).Type);
                return this;
            }
            
            
            public IXamlIlEmitter Emit(OpCode code, IXamlIlField field)
            {
                _ilg.Emit(code, ((SreField) field).Field);
                return this;
            }

            class SreLabel : IXamlIlLabel
            {
                public Label Label { get; }

                public SreLabel(Label label)
                {
                    Label = label;
                }
            }
            
            class SreLocal : IXamlIlLocal
            {
                public LocalBuilder Local { get; }

                public SreLocal(LocalBuilder local)
                {
                    Local = local;
                }
            }
        }

        class SreTypeBuilder : SreType, IXamlIlTypeBuilder
        {
            private readonly SreTypeSystem _system;
            private readonly TypeBuilder _tb;

            public SreTypeBuilder(SreTypeSystem system, TypeBuilder tb) : base(system,null, tb)
            {
                _system = system;
                _tb = tb;
            }
            
            public IXamlIlField DefineField(IXamlIlType type, string name, bool isPublic, bool isStatic)
            {
                var f = _tb.DefineField(name, ((SreType) type).Type,
                    (isPublic ? FieldAttributes.Public : FieldAttributes.Private)
                    | (isStatic ? FieldAttributes.Static : default(FieldAttributes)));
                return new SreField(_system, f);
            }

            public void AddInterfaceImplementation(IXamlIlType type)
            {
                _tb.AddInterfaceImplementation(((SreType)type).Type);
            }

            class SreMethodBuilder : SreMethod, IXamlIlMethodBuilder
            {
                public MethodBuilder MethodBuilder { get; }

                public SreMethodBuilder(SreTypeSystem system, MethodBuilder methodBuilder) : base(system, methodBuilder)
                {
                    MethodBuilder = methodBuilder;
                    Generator = new IlGen(system, methodBuilder.GetILGenerator());
                }

                public IXamlIlEmitter Generator { get; }

                public void EmitClosure(IEnumerable<IXamlIlType> fields)
                {
                    throw new NotImplementedException();
                }
            }
            
            public IXamlIlMethodBuilder DefineMethod(IXamlIlType returnType, IEnumerable<IXamlIlType> args, string name,
                bool isPublic, bool isStatic,
                bool isInterfaceImpl, IXamlIlMethod overrideMethod)
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

            public IXamlIlProperty DefineProperty(IXamlIlType propertyType, string name, IXamlIlMethod setter, IXamlIlMethod getter)
            {
                var p = _tb.DefineProperty(name, PropertyAttributes.None, ((SreType) propertyType).Type, new Type[0]);
                if (setter != null)
                    p.SetSetMethod(((SreMethodBuilder) setter).MethodBuilder);
                if (getter != null)
                    p.SetGetMethod(((SreMethodBuilder) getter).MethodBuilder);
                return new SreProperty(_system, p);
            }

            class SreConstructorBuilder : SreConstructor, IXamlIlConstructorBuilder
            {
                public SreConstructorBuilder(SreTypeSystem system, ConstructorBuilder ctor) : base(system, ctor)
                {
                    Generator = new IlGen(system, ctor.GetILGenerator());
                }

                public IXamlIlEmitter Generator { get; }
            }

            
            public IXamlIlConstructorBuilder DefineConstructor(bool isStatic, params IXamlIlType[] args)
            {
                var attrs = MethodAttributes.Public;
                if (isStatic)
                    attrs |= MethodAttributes.Static;
                var ctor = _tb.DefineConstructor(attrs,
                    CallingConventions.Standard,
                    args.Cast<SreType>().Select(t => t.Type).ToArray());
                return new SreConstructorBuilder(_system, ctor);
            }
            
            public IXamlIlType CreateType() => new SreType(_system, null, _tb.CreateTypeInfo());
            public IXamlIlTypeBuilder DefineSubType(IXamlIlType baseType, string name, bool isPublic)
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
        }

        public IXamlIlAssembly GetAssembly(Assembly asm) => ResolveAssembly(asm);
    }
}