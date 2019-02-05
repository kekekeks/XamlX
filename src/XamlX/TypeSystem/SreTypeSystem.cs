using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace XamlX.TypeSystem
{
    public class SreTypeSystem : IXamlXTypeSystem
    {
        private List<IXamlXAssembly> _assemblies = new List<IXamlXAssembly>();
        public IReadOnlyList<IXamlXAssembly> Assemblies => _assemblies;
        
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
        
        public IXamlXAssembly FindAssembly(string name)
        {
            return Assemblies.FirstOrDefault(a => a.Name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        SreAssembly ResolveAssembly(Assembly asm)
        {
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
                   (_customAttributes = Assembly.GetCustomAttributesData().Select(a => new SreCustomAttribute(a,
                       _system.ResolveType(a.AttributeType))).ToList());

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
                   (_customAttributes = _member.GetCustomAttributesData().Select(a => new SreCustomAttribute(a,
                       System.ResolveType(a.AttributeType))).ToList());
        }
        
        class SreType : SreMemberInfo, IXamlXType
        {
            private IReadOnlyList<IXamlXProperty> _properties;
            private IReadOnlyList<IXamlXField> _fields;
            private IReadOnlyList<IXamlXMethod> _methods;
            private IReadOnlyList<IXamlXConstructor> _constructors;
            public Type Type { get; }

            public SreType(SreTypeSystem system, SreAssembly asm, Type type): base(system, type)
            {
                Assembly = asm;
                Type = type;
            }

            public bool Equals(IXamlXType other) => Type == (other as SreType)?.Type;

            public object Id => Type;
            
            public string Namespace => Type.Namespace;
            public IXamlXAssembly Assembly { get; }

            public IReadOnlyList<IXamlXProperty> Properties =>
                _properties ?? (_properties = Type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                   BindingFlags.NonPublic)
                    .Select(p => new SreProperty(System, p)).ToList());

            public IReadOnlyList<IXamlXField> Fields =>
                _fields ?? (_fields = Type.GetFields(BindingFlags.Public | BindingFlags.Static
                                                                         | BindingFlags.Instance |
                                                                         BindingFlags.NonPublic)
                    .Select(f => new SreField(System, f)).ToList());

            public IReadOnlyList<IXamlXMethod> Methods =>
                _methods ?? (_methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Static |
                                                        BindingFlags.Instance)
                    .Select(m => new SreMethod(System, m)).ToList());

            public IReadOnlyList<IXamlXConstructor> Constructors =>
                _constructors ?? (_constructors = Type.GetConstructors(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(c => new SreConstructor(System, c)).ToList());

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
                return Type.IsAssignableFrom(((SreType) type).Type);
            }

            public IXamlXType MakeGenericType(IReadOnlyList<IXamlXType> typeArguments)
            {
                return System.ResolveType(
                    Type.MakeGenericType(typeArguments.Select(t => ((SreType) t).Type).ToArray()));
            }

            public IXamlXType BaseType => Type.BaseType == null ? null : System.ResolveType(Type.BaseType);
            public bool IsValueType => Type.IsValueType;
            public bool IsEnum => Type.IsEnum;
            public IXamlXType GetEnumUnderlyingType()
            {
                return System.ResolveType(Enum.GetUnderlyingType(Type));
            }
        }

        class SreCustomAttribute : IXamlXCustomAttribute
        {
            private readonly CustomAttributeData _data;

            public SreCustomAttribute(CustomAttributeData data, IXamlXType type)
            {
                Type = type;
                _data = data;
                Parameters = data.ConstructorArguments.Select(p => p.Value).ToList();
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
        }
        
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

            public bool Equals(IXamlXProperty other) => ((SreProperty) other)?.Member.Equals(Member) == true;

            public IXamlXType PropertyType => System.ResolveType(Member.PropertyType);
            public IXamlXMethod Setter { get; }
            public IXamlXMethod Getter { get; }
        }
        
        class SreField : SreMemberInfo, IXamlXField
        {
            public FieldInfo Field { get; }

            private IReadOnlyList<IXamlXType> _parameters;
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

            public bool Equals(IXamlXField other) => ((SreField) other)?.Field.Equals(Field) == true;
        }

        public IXamlXCodeGen CreateCodeGen(MethodBuilder mb)
        {
            return new CodeGen(mb);
        }

        class IlGen : IXamlXEmitter
        {
            private readonly ILGenerator _ilg;

            public IlGen(ILGenerator ilg)
            {
                _ilg = ilg;
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
        }

        class CodeGen : IXamlXCodeGen
        {
            public CodeGen(MethodBuilder mb)
            {
                Generator = new IlGen(mb.GetILGenerator());
            }
            public IXamlXEmitter Generator { get; }
            public void EmitClosure(IEnumerable<IXamlXType> fields)
            {
                throw new NotImplementedException();
            }
        }
    }
}