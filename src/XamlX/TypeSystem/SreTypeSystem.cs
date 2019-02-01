using System;
using System.Collections.Generic;
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
                   (_customAttributes = Assembly.GetCustomAttributesData().Select(a => new SreCustomAttribute(a,
                       _system.ResolveType(a.AttributeType))).ToList());

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
                   (_customAttributes = _member.GetCustomAttributesData().Select(a => new SreCustomAttribute(a,
                       System.ResolveType(a.AttributeType))).ToList());
        }
        
        class SreType : SreMemberInfo, IXamlType
        {
            private IReadOnlyList<IXamlProperty> _properties;
            private IReadOnlyList<IXamlMethod> _methods;
            private IReadOnlyList<IXamlConstructor> _constructors;
            public Type Type { get; }

            public SreType(SreTypeSystem system, SreAssembly asm, Type type): base(system, type)
            {
                Assembly = asm;
                Type = type;
            }
                       
            public bool Equals(IXamlType other) => Type == ((SreType) other)?.Type;

            public object Id => Type;
            
            public string Namespace => Type.Namespace;
            public IXamlAssembly Assembly { get; }

            public IReadOnlyList<IXamlProperty> Properties =>
                _properties ?? (_properties = Type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                                   BindingFlags.NonPublic)
                    .Select(p => new SreProperty(System, p)).ToList());

            public IReadOnlyList<IXamlMethod> Methods =>
                _methods ?? (_methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Static |
                                                        BindingFlags.Instance)
                    .Select(m => new SreMethod(System, m)).ToList());

            public IReadOnlyList<IXamlConstructor> Constructors =>
                _constructors ?? (_constructors = Type.GetConstructors(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(c => new SreConstructor(System, c)).ToList());

            public bool IsAssignableFrom(IXamlType type)
            {
                if (type == XamlXNullType.Instance)
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

            public IXamlType BaseType => Type.BaseType == null ? null : System.ResolveType(Type.BaseType);
        }

        class SreCustomAttribute : IXamlCustomAttribute
        {
            private readonly CustomAttributeData _data;

            public SreCustomAttribute(CustomAttributeData data, IXamlType type)
            {
                Type = type;
                _data = data;
                Parameters = data.ConstructorArguments.Select(p => p.Value).ToList();
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

            public bool Equals(IXamlProperty other) => ((SreProperty) other)?.Member.Equals(Member) == true;

            public IXamlType PropertyType => System.ResolveType(Member.PropertyType);
            public IXamlMethod Setter { get; }
            public IXamlMethod Getter { get; }
        }

        public IXamlXCodeGen CreateCodeGen(MethodBuilder mb)
        {
            return new CodeGen(mb);
        }

        class IlGen : IXamlILEmitter
        {
            private readonly ILGenerator _ilg;

            public IlGen(ILGenerator ilg)
            {
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

            public IXamlILEmitter Emit(OpCode code, IXamlType type)
            {
                _ilg.Emit(code, ((SreType) type).Type);
                return this;
            }
        }

        class CodeGen : IXamlXCodeGen
        {
            public CodeGen(MethodBuilder mb)
            {
                Generator = new IlGen(mb.GetILGenerator());
            }
            public IXamlILEmitter Generator { get; }
            public void EmitClosure(IEnumerable<IXamlType> fields)
            {
                throw new NotImplementedException();
            }
        }
    }
}