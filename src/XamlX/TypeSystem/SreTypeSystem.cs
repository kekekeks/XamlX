using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace XamlX.TypeSystem
{
    public class SreTypeSystem : IXamlTypeSystem
    {
        private List<IXamlAssembly> _assemblies = new List<IXamlAssembly>();
        public IReadOnlyList<IXamlAssembly> Assemblies => _assemblies;
        
        private Dictionary<Type, SreType> _typeDic = new Dictionary<Type, SreType>();

        public SreTypeSystem()
        {
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
        
        public IXamlAssembly FindAssembly(string name)
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

        public IXamlType FindType(string name)
        {
            foreach (var asm in Assemblies)
            {
                var t = asm.FindType(name);
                if (t != null)
                    return t;
            }

            return null;
        }

        class SreAssembly : IXamlAssembly
        {
            private readonly SreTypeSystem _system;
            private IReadOnlyList<IXamlCustomAttribute> _customAttributes;
            public Assembly Assembly { get; }

            public SreAssembly(SreTypeSystem system, Assembly asm)
            {
                _system = system;
                Assembly = asm;
            }


            public bool Equals(IXamlAssembly other) => Assembly == ((SreAssembly) other)?.Assembly;

            public string Name => Assembly.GetName().Name;
            
            public IReadOnlyList<IXamlType> Types { get; private set; }
            private Dictionary<string, SreType> _typeDic = new Dictionary<string, SreType>();

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes
                => _customAttributes ??
                   (_customAttributes = Assembly.GetCustomAttributesData().Select(a => new SreCustomAttribute(
                       _system, a, _system.ResolveType(a.AttributeType))).ToList());

            public IXamlType FindType(string fullName)
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
            private  IReadOnlyList<IXamlCustomAttribute> _customAttributes;
            public string Name => _member.Name;
            public SreMemberInfo(SreTypeSystem system, MemberInfo member)
            {
                System = system;
                _member = member;
            }

            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes
                => _customAttributes ??
                   (_customAttributes = _member.GetCustomAttributesData().Select(a => new SreCustomAttribute(System,
                       a, System.ResolveType(a.AttributeType))).ToList());
        }
        
        [DebuggerDisplay("{" + nameof(Type) + "}")]
        class SreType : SreMemberInfo, IXamlType
        {
            private IReadOnlyList<IXamlProperty> _properties;
            private IReadOnlyList<IXamlField> _fields;
            private IReadOnlyList<IXamlMethod> _methods;
            private IReadOnlyList<IXamlConstructor> _constructors;
            private IReadOnlyList<IXamlType> _genericArguments;
            private IReadOnlyList<IXamlType> _interfaces;
            public Type Type { get; }

            public SreType(SreTypeSystem system, SreAssembly asm, Type type): base(system, type)
            {
                Assembly = asm;
                Type = type;
            }

            public bool Equals(IXamlType other) => Type == (other as SreType)?.Type;

            public object Id => Type;
            
            public string Namespace => Type.Namespace;
            public IXamlAssembly Assembly { get; }

            public IReadOnlyList<IXamlProperty> Properties =>
                _properties ?? (_properties = Type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                   BindingFlags.NonPublic)
                    .Select(p => new SreProperty(System, p)).ToList());

            public IReadOnlyList<IXamlField> Fields =>
                _fields ?? (_fields = Type.GetFields(BindingFlags.Public | BindingFlags.Static
                                                                         | BindingFlags.Instance |
                                                                         BindingFlags.NonPublic)
                    .Select(f => new SreField(System, f)).ToList());

            public IReadOnlyList<IXamlMethod> Methods =>
                _methods ?? (_methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Static |
                                                        BindingFlags.Instance)
                    .Select(m => new SreMethod(System, m)).ToList());

            public IReadOnlyList<IXamlConstructor> Constructors =>
                _constructors ?? (_constructors = Type.GetConstructors(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(c => new SreConstructor(System, c)).ToList());

            public IReadOnlyList<IXamlType> Interfaces =>
                _interfaces ?? (_interfaces = Type.GetInterfaces().Select(System.ResolveType).ToList());

            public IReadOnlyList<IXamlType> GenericArguments
            {
                get
                {
                    if (_genericArguments != null)
                        return _genericArguments;
                    if (GenericTypeDefinition == null)
                        return _genericArguments = new IXamlType[0];
                    return _genericArguments = Type.GetGenericArguments().Select(System.ResolveType).ToList();
                }
            }

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
                return Type.IsAssignableFrom(((SreType) type).Type);
            }

            public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments)
            {
                return System.ResolveType(
                    Type.MakeGenericType(typeArguments.Select(t => ((SreType) t).Type).ToArray()));
            }

            public IXamlType GenericTypeDefinition => Type.IsConstructedGenericType
                ? System.ResolveType(Type.GetGenericTypeDefinition())
                : null;

            public IXamlType BaseType => Type.BaseType == null ? null : System.ResolveType(Type.BaseType);
            public bool IsValueType => Type.IsValueType;
            public bool IsEnum => Type.IsEnum;
            public IXamlType GetEnumUnderlyingType()
            {
                return System.ResolveType(Enum.GetUnderlyingType(Type));
            }
        }

        class SreCustomAttribute : IXamlCustomAttribute
        {
            private readonly CustomAttributeData _data;

            public SreCustomAttribute(SreTypeSystem system, CustomAttributeData data, IXamlType type)
            {
                Type = type;
                _data = data;
                Parameters = data.ConstructorArguments.Select(p =>
                    p.Value is Type t ? system.ResolveType(t) : p.Value
                ).ToList();
                Properties = data.NamedArguments?.ToDictionary(x => x.MemberName, x => x.TypedValue.Value) ??
                             new Dictionary<string, object>();
            }
            
            public bool Equals(IXamlCustomAttribute other)
            {
                return ((SreCustomAttribute) other)?._data.Equals(_data) == true;
            }

            public IXamlType Type { get; }
            public List<object> Parameters { get; }
            public Dictionary<string, object> Properties { get; }
        }

        [DebuggerDisplay("{_method}")]
        class SreMethodBase : SreMemberInfo
        {
            private readonly MethodBase _method;

            private IReadOnlyList<IXamlType> _parameters;
            public SreMethodBase(SreTypeSystem system, MethodBase method) : base(system, method)
            {
                _method = method;
            }
            public bool IsPublic => _method.IsPublic;
            public bool IsStatic => _method.IsStatic;
            public IReadOnlyList<IXamlType> Parameters =>
                _parameters ?? (_parameters = _method.GetParameters()
                    .Select(p => System.ResolveType(p.ParameterType)).ToList());
        }
        
        [DebuggerDisplay("{Method}")]
        class SreMethod : SreMethodBase, IXamlMethod
        {
            public MethodInfo Method { get; }
            private readonly SreTypeSystem _system;

            public SreMethod(SreTypeSystem system, MethodInfo method) : base(system, method)
            {
                Method = method;
                _system = system;
            }

            public bool Equals(IXamlMethod other) => ((SreMethod) other)?.Method.Equals(Method) == true;
            public IXamlType ReturnType => _system.ResolveType(Method.ReturnType);
            public IXamlType DeclaringType => _system.ResolveType(Method.DeclaringType);
        }

        class SreConstructor : SreMethodBase, IXamlConstructor
        {
            public ConstructorInfo Constuctor { get; }
            public SreConstructor(SreTypeSystem system, ConstructorInfo ctor) : base(system, ctor)
            {
                Constuctor = ctor;
            }

            public bool Equals(IXamlConstructor other) 
                => ((SreConstructor) other)?.Constuctor.Equals(Constuctor) == true;
        }

        class SreProperty : SreMemberInfo, IXamlProperty
        {
            public PropertyInfo Member { get; }

            public SreProperty(SreTypeSystem system, PropertyInfo member) : base(system, member)
            {
                Member = member;
                Setter = member.SetMethod == null ? null : new SreMethod(system, member.SetMethod);
                Getter = member.GetMethod == null ? null : new SreMethod(system, member.GetMethod);
            }

            public bool Equals(IXamlProperty other)
            {
                var otherProp =((SreProperty) other)?.Member;
                if (otherProp == null)
                    return false;
                return otherProp?.DeclaringType?.Equals(Member.DeclaringType) == true
                       && Member.Name == otherProp.Name;
            }

            public IXamlType PropertyType => System.ResolveType(Member.PropertyType);
            public IXamlMethod Setter { get; }
            public IXamlMethod Getter { get; }
        }
        
        class SreField : SreMemberInfo, IXamlField
        {
            public FieldInfo Field { get; }

            private IReadOnlyList<IXamlType> _parameters;
            public SreField(SreTypeSystem system, FieldInfo field) : base(system, field)
            {
                Field = field;
                FieldType = system.ResolveType(field.FieldType);
            }

            public IXamlType FieldType { get; }
            public bool IsPublic => Field.IsPublic;
            public bool IsStatic => Field.IsStatic;
            public bool IsLiteral => Field.IsLiteral;
            public object GetLiteralValue()
            {
                if (!IsLiteral)
                    throw new InvalidOperationException();
                return Field.GetValue(null);
            }

            public bool Equals(IXamlField other) => ((SreField) other)?.Field.Equals(Field) == true;
        }

        public IXamlXCodeGen CreateCodeGen(MethodBuilder mb)
        {
            return new CodeGen(this, mb);
        }

        public Type GetType(IXamlType t) => ((SreType) t).Type;
        public IXamlType GetType(Type t) => ResolveType(t);

        public IXamlTypeBuilder CreateTypeBuilder(TypeBuilder builder) => new SreTypeBuilder(this, builder);

        class IlGen : IXamlILEmitter
        {
            private readonly ILGenerator _ilg;
            public IXamlTypeSystem TypeSystem { get; }

            public IlGen(SreTypeSystem system, ILGenerator ilg)
            {
                TypeSystem = system;
                _ilg = ilg;
            }

            public IXamlILEmitter Emit(OpCode code)
            {
                _ilg.Emit(code);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, IXamlMethod method)
            {
                _ilg.Emit(code, ((SreMethod) method).Method);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, IXamlConstructor ctor)
            {
                _ilg.Emit(code, ((SreConstructor) ctor).Constuctor);
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
                return new SreLocal(_ilg.DeclareLocal(((SreType) type).Type));
            }

            public IXamlLabel DefineLabel()
            {
                return new SreLabel(_ilg.DefineLabel());
            }

            public IXamlILEmitter MarkLabel(IXamlLabel label)
            {
                _ilg.MarkLabel(((SreLabel) label).Label);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, IXamlLabel label)
            {
                _ilg.Emit(code, ((SreLabel)label).Label);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, IXamlLocal local)
            {
                _ilg.Emit(code, ((SreLocal) local).Local);
                return this;
            }

            public IXamlILEmitter Emit(OpCode code, IXamlType type)
            {
                _ilg.Emit(code, ((SreType) type).Type);
                return this;
            }
            
            
            public IXamlILEmitter Emit(OpCode code, IXamlField field)
            {
                _ilg.Emit(code, ((SreField) field).Field);
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
            
            class SreLocal : IXamlLocal
            {
                public LocalBuilder Local { get; }

                public SreLocal(LocalBuilder local)
                {
                    Local = local;
                }
            }
        }

        class SreTypeBuilder : SreType, IXamlTypeBuilder
        {
            private readonly SreTypeSystem _system;
            private readonly TypeBuilder _tb;

            public SreTypeBuilder(SreTypeSystem system, TypeBuilder tb) : base(system,null, tb)
            {
                _system = system;
                _tb = tb;
            }
            
            public IXamlField DefineField(IXamlType type, string name, bool isPublic, bool isStatic)
            {
                var f = _tb.DefineField(name, ((SreType) type).Type,
                    (isPublic ? FieldAttributes.Public : FieldAttributes.Private)
                    | (isStatic ? FieldAttributes.Static : default(FieldAttributes)));
                return new SreField(_system, f);
            }

            public void AddInterfaceImplementation(IXamlType type)
            {
                _tb.AddInterfaceImplementation(((SreType)type).Type);
            }

            class SreMethodBuilder : SreMethod, IXamlMethodBuilder
            {
                public MethodBuilder MethodBuilder { get; }

                public SreMethodBuilder(SreTypeSystem system, MethodBuilder methodBuilder) : base(system, methodBuilder)
                {
                    MethodBuilder = methodBuilder;
                    Generator = new IlGen(system, methodBuilder.GetILGenerator());
                }

                public IXamlILEmitter Generator { get; }

                public void EmitClosure(IEnumerable<IXamlType> fields)
                {
                    throw new NotImplementedException();
                }
            }
            
            public IXamlMethodBuilder DefineMethod(IXamlType returnType, IEnumerable<IXamlType> args, string name,
                bool isPublic, bool isStatic,
                bool isInterfaceImpl, IXamlMethod overrideMethod)
            {
                var ret = ((SreType) returnType).Type;
                var argTypes = args.Cast<SreType>().Select(t => t.Type);
                var m = _tb.DefineMethod(name, 
                    (isPublic ? MethodAttributes.Public : MethodAttributes.Private)
                    |(isStatic ? MethodAttributes.Static : default(MethodAttributes))
                    |(isInterfaceImpl ? MethodAttributes.Virtual|MethodAttributes.NewSlot : default(MethodAttributes))
                    , ret, argTypes.ToArray());
                if (overrideMethod != null)
                    _tb.DefineMethodOverride(m, ((SreMethod) overrideMethod).Method);
               
                return new SreMethodBuilder(_system, m);
            }

            public IXamlProperty DefineProperty(IXamlType propertyType, string name, IXamlMethod setter, IXamlMethod getter)
            {
                var p = _tb.DefineProperty(name, PropertyAttributes.None, ((SreType) propertyType).Type, new Type[0]);
                if (setter != null)
                    p.SetSetMethod(((SreMethodBuilder) setter).MethodBuilder);
                if (getter != null)
                    p.SetGetMethod(((SreMethodBuilder) getter).MethodBuilder);
                return new SreProperty(_system, p);
            }

            class SreConstructorBuilder : SreConstructor, IXamlConstructorBuilder
            {
                public SreConstructorBuilder(SreTypeSystem system, ConstructorBuilder ctor) : base(system, ctor)
                {
                    Generator = new IlGen(system, ctor.GetILGenerator());
                }

                public IXamlILEmitter Generator { get; }
            }

            
            public IXamlConstructorBuilder DefineConstructor(params IXamlType[] args)
            {
                var ctor = _tb.DefineConstructor(MethodAttributes.Public,
                    CallingConventions.Standard,
                    args.Cast<SreType>().Select(t => t.Type).ToArray());
                return new SreConstructorBuilder(_system, ctor);
            }
            
            public IXamlType CreateType() => new SreType(_system, null, _tb.CreateTypeInfo());
            public IXamlTypeBuilder DefineSubType(IXamlType baseType, string name, bool isPublic)
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

        class CodeGen : IXamlXCodeGen
        {
            public CodeGen(SreTypeSystem system, MethodBuilder mb)
            {
                Generator = new IlGen(system, mb.GetILGenerator());
            }
            public IXamlILEmitter Generator { get; }
            public void EmitClosure(IEnumerable<IXamlType> fields)
            {
                throw new NotImplementedException();
            }
        }
    }
}