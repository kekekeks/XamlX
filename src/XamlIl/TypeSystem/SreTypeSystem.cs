using System;
using System.Collections.Generic;
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
                   (_customAttributes = Assembly.GetCustomAttributesData().Select(a => new SreCustomAttribute(a,
                       _system.ResolveType(a.AttributeType))).ToList());

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
                   (_customAttributes = _member.GetCustomAttributesData().Select(a => new SreCustomAttribute(a,
                       System.ResolveType(a.AttributeType))).ToList());
        }
        
        class SreType : SreMemberInfo, IXamlIlType
        {
            private IReadOnlyList<IXamlIlProperty> _properties;
            private IReadOnlyList<IXamlIlMethod> _methods;
            private IReadOnlyList<IXamlIlConstructor> _constructors;
            public Type Type { get; }

            public SreType(SreTypeSystem system, SreAssembly asm, Type type): base(system, type)
            {
                Assembly = asm;
                Type = type;
            }
                       
            public bool Equals(IXamlIlType other) => Type == ((SreType) other)?.Type;

            public object Id => Type;
            
            public string Namespace => Type.Namespace;
            public IXamlIlAssembly Assembly { get; }

            public IReadOnlyList<IXamlIlProperty> Properties =>
                _properties ?? (_properties = Type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                   BindingFlags.NonPublic)
                    .Select(p => new SreProperty(System, p)).ToList());

            public IReadOnlyList<IXamlIlMethod> Methods =>
                _methods ?? (_methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Static |
                                                        BindingFlags.Instance)
                    .Select(m => new SreMethod(System, m)).ToList());

            public IReadOnlyList<IXamlIlConstructor> Constructors =>
                _constructors ?? (_constructors = Type.GetConstructors(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(c => new SreConstructor(System, c)).ToList());

            public bool IsAssignableFrom(IXamlIlType type)
            {
                if (type == XamlIlNullType.Instance)
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

            public IXamlIlType BaseType => Type.BaseType == null ? null : System.ResolveType(Type.BaseType);
        }

        class SreCustomAttribute : IXamlIlCustomAttribute
        {
            private readonly CustomAttributeData _data;

            public SreCustomAttribute(CustomAttributeData data, IXamlIlType type)
            {
                Type = type;
                _data = data;
                Parameters = data.ConstructorArguments.Select(p => p.Value).ToList();
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

            public bool Equals(IXamlIlProperty other) => ((SreProperty) other)?.Member.Equals(Member) == true;

            public IXamlIlType PropertyType => System.ResolveType(Member.PropertyType);
            public IXamlIlMethod Setter { get; }
            public IXamlIlMethod Getter { get; }
        }

        public IXamlIlCodeGen CreateCodeGen(MethodBuilder mb)
        {
            return new CodeGen(mb);
        }

        class IlGen : IXamlIlEmitter
        {
            private readonly ILGenerator _ilg;

            public IlGen(ILGenerator ilg)
            {
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

            public IXamlIlEmitter Emit(OpCode code, IXamlIlType type)
            {
                _ilg.Emit(code, ((SreType) type).Type);
                return this;
            }
        }

        class CodeGen : IXamlIlCodeGen
        {
            public CodeGen(MethodBuilder mb)
            {
                Generator = new IlGen(mb.GetILGenerator());
            }
            public IXamlIlEmitter Generator { get; }
            public void EmitClosure(IEnumerable<IXamlIlType> fields)
            {
                throw new NotImplementedException();
            }
        }
    }
}