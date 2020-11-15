using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;

namespace XamlX.TypeSystem
{
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlType : IEquatable<IXamlType>
    {
        object Id { get; }
        string Name { get; }
        string Namespace { get; }
        string FullName { get; }
        IXamlAssembly Assembly { get; }
        IReadOnlyList<IXamlProperty> Properties { get; }
        IReadOnlyList<IXamlEventInfo> Events { get; }
        IReadOnlyList<IXamlField> Fields { get; }
        IReadOnlyList<IXamlMethod> Methods { get; }
        IReadOnlyList<IXamlConstructor> Constructors { get; }
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
        IReadOnlyList<IXamlType> GenericArguments { get; }
        bool IsAssignableFrom(IXamlType type);
        IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments);
        IXamlType GenericTypeDefinition { get; }
        bool IsArray { get; }
        IXamlType ArrayElementType { get; }
        IXamlType MakeArrayType(int dimensions);
        IXamlType BaseType { get; }
        bool IsValueType { get; }
        bool IsEnum { get; }
        IReadOnlyList<IXamlType> Interfaces { get; }
        bool IsInterface { get; }
        IXamlType GetEnumUnderlyingType();
        IReadOnlyList<IXamlType> GenericParameters { get; }
        int GetHashCode();
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlMethod : IEquatable<IXamlMethod>, IXamlMember
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlType ReturnType { get; }
        IReadOnlyList<IXamlType> Parameters { get; }
        IXamlType DeclaringType { get; }
        IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments);
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlMember
    {
        string Name { get; }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlConstructor : IEquatable<IXamlConstructor>
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IReadOnlyList<IXamlType> Parameters { get; }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlProperty : IEquatable<IXamlProperty>, IXamlMember
    {
        IXamlType PropertyType { get; }
        IXamlMethod Setter { get; }
        IXamlMethod Getter { get; }
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
        IReadOnlyList<IXamlType> IndexerParameters { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlEventInfo : IEquatable<IXamlEventInfo>, IXamlMember
    {
        IXamlMethod Add { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlField : IEquatable<IXamlField>, IXamlMember
    {
        IXamlType FieldType { get; }
        bool IsPublic { get; }
        bool IsStatic { get; }
        bool IsLiteral { get; }
        object GetLiteralValue();
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAssembly : IEquatable<IXamlAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
        IXamlType FindType(string fullName);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlCustomAttribute : IEquatable<IXamlCustomAttribute>
    {
        IXamlType Type { get; }
        List<object> Parameters { get; }
        Dictionary<string, object> Properties { get; }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlTypeSystem
    {
        IReadOnlyList<IXamlAssembly> Assemblies { get; }
        IXamlAssembly FindAssembly(string substring);
        IXamlType FindType(string name);
        IXamlType FindType(string name, string assembly);
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IFileSource
    {
        string FilePath { get; }
        byte[] FileContents { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlLocal
    {
        
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlLabel
    {
        
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlTypeBuilder<TBackendEmitter> : IXamlType
    {
        IXamlField DefineField(IXamlType type, string name, bool isPublic, bool isStatic);
        void AddInterfaceImplementation(IXamlType type);

        IXamlMethodBuilder<TBackendEmitter> DefineMethod(IXamlType returnType, IEnumerable<IXamlType> args, string name, bool isPublic, bool isStatic,
            bool isInterfaceImpl, IXamlMethod overrideMethod = null);

        IXamlProperty DefineProperty(IXamlType propertyType, string name, IXamlMethod setter, IXamlMethod getter);
        IXamlConstructorBuilder<TBackendEmitter> DefineConstructor(bool isStatic, params IXamlType[] args);
        IXamlType CreateType();
        IXamlTypeBuilder<TBackendEmitter> DefineSubType(IXamlType baseType, string name, bool isPublic);
        void DefineGenericParameters(IReadOnlyList<KeyValuePair<string, XamlGenericParameterConstraint>> names);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlMethodBuilder<TBackendEmitter> : IXamlMethod
    {
        TBackendEmitter Generator { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlConstructorBuilder<TBackendEmitter> : IXamlConstructor
    {
        TBackendEmitter Generator { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    struct XamlGenericParameterConstraint
    {
        public bool IsClass { get; set; }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlPseudoType : IXamlType
    {
        public XamlPseudoType(string name)
        {
            Name = name;
        }
        public bool Equals(IXamlType other) => other == this;

        public object Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public string Namespace { get; } = "";
        public string FullName => Name;
        public IXamlAssembly Assembly { get; } = null;
        public IReadOnlyList<IXamlProperty> Properties { get; } = new IXamlProperty[0];
        public IReadOnlyList<IXamlEventInfo> Events { get; } = new IXamlEventInfo[0];
        public IReadOnlyList<IXamlField> Fields { get; } = new List<IXamlField>();
        public IReadOnlyList<IXamlMethod> Methods { get; } = new IXamlMethod[0];
        public IReadOnlyList<IXamlConstructor> Constructors { get; } = new IXamlConstructor[0];
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; } = new IXamlCustomAttribute[0];
        public IReadOnlyList<IXamlType> GenericArguments { get; } = new IXamlType[0];
        public IXamlType MakeArrayType(int dimensions) => throw new NullReferenceException();

        public IXamlType BaseType { get; }
        public bool IsValueType { get; } = false;
        public bool IsEnum { get; } = false;
        public IReadOnlyList<IXamlType> Interfaces { get; } = new IXamlType[0];
        public bool IsInterface => false;
        public IXamlType GetEnumUnderlyingType() => throw new InvalidOperationException();
        public IReadOnlyList<IXamlType> GenericParameters { get; } = null;

        public bool IsAssignableFrom(IXamlType type) => type == this;

        public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments)
        {
            throw new NotSupportedException();
        }

        public IXamlType GenericTypeDefinition => null;
        public bool IsArray { get; }
        public IXamlType ArrayElementType { get; }
        public static XamlPseudoType Null { get; } = new XamlPseudoType("{x:Null}");
        public static XamlPseudoType Unknown { get; } = new XamlPseudoType("{Unknown type}");

        public static XamlPseudoType Unresolved(string message) =>
            new XamlPseudoType($"{{Unresolved type: '{message}'}}");
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    class FindMethodMethodSignature
    {
        public string Name { get; set; }
        public IXamlType ReturnType { get; set; }
        public bool IsStatic { get; set; }
        public bool IsExactMatch { get; set; } = true;
        public bool DeclaringOnly { get; set; } = false;
        public IReadOnlyList<IXamlType> Parameters { get; set; }

        public FindMethodMethodSignature(string name, IXamlType returnType, params IXamlType[] parameters)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return
                $"{(IsStatic ? "static" : "instance")} {ReturnType.GetFullName()} {Name} ({string.Join(", ", Parameters.Select(p => p.GetFullName()))}) (exact match: {IsExactMatch}, declaring only: {DeclaringOnly})";
        }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    static class XamlTypeSystemExtensions
    {
        public static string GetFqn(this IXamlType type) => $"{type.Assembly?.Name}:{type.Namespace}.{type.Name}";

        public static string GetFullName(this IXamlType type)
        {
            var name = type.Name;
            if (type.Namespace != null)
                name = type.Namespace + "." + name;
            if (type.Assembly != null)
                name += "," + type.Assembly.Name;
            return name;
        }
        
        public static IXamlType GetType(this IXamlTypeSystem sys, string type)
        {
            var f = sys.FindType(type);
            if (f == null)
                throw new XamlTypeSystemException("Unable to resolve type " + type);
            return f;
        }
        
        public static IEnumerable<IXamlMethod> FindMethods(this IXamlType type, Func<IXamlMethod, bool> criteria)
        {
            foreach (var m in type.Methods)
                if (criteria(m))
                    yield return m;
            var bt = type.BaseType;
            if(bt!=null)
                foreach (var bm in bt.FindMethods(criteria))
                    yield return bm;
            foreach(var iface in type.Interfaces)
            foreach(var m in iface.Methods)
                if (criteria(m))
                    yield return m;
        }

        public static IXamlMethod FindMethod(this IXamlType type, Func<IXamlMethod, bool> criteria)
        {
            foreach (var m in type.Methods)
                if (criteria(m))
                    return m;
            var bres = type.BaseType?.FindMethod(criteria);
            if (bres != null)
                return bres;
            foreach(var iface in type.Interfaces)
                foreach(var m in iface.Methods)
                    if (criteria(m))
                        return m;
            return null;
        }
        
        public static IXamlMethod FindMethod(this IXamlType type, string name, IXamlType returnType, 
            bool allowDowncast, params IXamlType[] args)
        {
            foreach (var m in type.Methods)
            {
                if (m.Name == name && m.ReturnType.Equals(returnType) && m.Parameters.Count == args.Length)
                {
                    var mismatch = false;
                    for (var c = 0; c < args.Length; c++)
                    {
                        if (allowDowncast)
                            mismatch = !m.Parameters[c].IsAssignableFrom(args[c]);
                        else
                            mismatch = !m.Parameters[c].Equals(args[c]);
                        if(mismatch)
                            break;
                    }

                    if (!mismatch)
                        return m;
                }
            }

            if (type.BaseType != null)
                return FindMethod(type.BaseType, name, returnType, allowDowncast, args);
            return null;
        }

        public static IXamlMethod GetMethod(this IXamlType type, FindMethodMethodSignature signature)
        {
            var found = FindMethod(type, signature);
            if (found == null)
                throw new XamlTypeSystemException($"Method with signature {signature} is not found on type {type.GetFqn()}");
            return found;
        }
        public static IXamlMethod FindMethod(this IXamlType type, FindMethodMethodSignature signature)
        {
            foreach (var m in type.Methods)
            {
                if (m.Name == signature.Name 
                    && m.ReturnType.Equals(signature.ReturnType) 
                    && m.Parameters.Count == signature.Parameters.Count
                    && m.IsStatic == signature.IsStatic
                    )
                {
                    var mismatch = false;
                    for (var c = 0; c < signature.Parameters.Count; c++)
                    {
                        if (!signature.IsExactMatch)
                            mismatch = !m.Parameters[c].IsAssignableFrom(signature.Parameters[c]);
                        else
                            mismatch = !m.Parameters[c].Equals(signature.Parameters[c]);
                        if(mismatch)
                            break;
                    }

                    if (!mismatch)
                        return m;
                }
            }

            if (type.BaseType != null && !signature.DeclaringOnly)
                return FindMethod(type.BaseType, signature);
            return null;
        }
        
        public static IXamlConstructor GetConstructor(this IXamlType type, List<IXamlType> args = null)
        {
            var found = FindConstructor(type, args);
            if (found == null)
            {
                if (args != null && args.Count > 0)
                {
                    var argsString = string.Join(", ", args.Select(a => a.GetFullName()));
                    
                    throw new XamlTypeSystemException($"Constructor with arguments {argsString} is not found on type {type.GetFqn()}");
                }
                
                throw new XamlTypeSystemException($"Constructor with no arguments is not found on type {type.GetFqn()}");
            }
            return found;
        }
        
        public static IXamlConstructor FindConstructor(this IXamlType type, List<IXamlType> args = null)
        {
            if(args == null)
                args = new List<IXamlType>();
            foreach (var ctor in type.Constructors.Where(c => c.IsPublic
                                                              && !c.IsStatic
                                                              && c.Parameters.Count == args.Count))
            {
                var mismatch = false;
                for (var c = 0; c < args.Count; c++)
                {
                    mismatch = !ctor.Parameters[c].IsAssignableFrom(args[c]);
                    if(mismatch)
                        break;
                }

                if (!mismatch)
                    return ctor;
            }

            return null;
        }

        public static bool IsNullable(this IXamlType type)
        {
            var def = type.GenericTypeDefinition;
            if (def == null) return false;
            return def.Namespace == "System" && def.Name == "Nullable`1";
        }

        public static bool IsNullableOf(this IXamlType type, IXamlType vtype)
        {
            return type.IsNullable() && type.GenericArguments[0].Equals(vtype);
        }

        public static IXamlType MakeGenericType(this IXamlType type, params IXamlType[] typeArguments)
            => type.MakeGenericType(typeArguments);

        public static IEnumerable<IXamlType> GetAllInterfaces(this IXamlType type)
        {
            foreach (var i in type.Interfaces)
                yield return i;
            if(type.BaseType!=null)
                foreach (var i in type.BaseType.GetAllInterfaces())
                    yield return i;
        }

        public static IEnumerable<IXamlProperty> GetAllProperties(this IXamlType t)
        {
            foreach (var p in t.Properties)
                yield return p;
            if(t.BaseType!=null)
                foreach (var p in t.BaseType.GetAllProperties())
                    yield return p;
        }

        public static IEnumerable<IXamlField> GetAllFields(this IXamlType t)
        {
            foreach (var p in t.Fields)
                yield return p;
            if (t.BaseType != null)
                foreach (var p in t.BaseType.GetAllFields())
                    yield return p;
        }

        public static IEnumerable<IXamlEventInfo> GetAllEvents(this IXamlType t)
        {
            foreach (var p in t.Events)
                yield return p;
            if(t.BaseType!=null)
                foreach (var p in t.BaseType.GetAllEvents())
                    yield return p;
        }

        public static bool IsDirectlyAssignableFrom(this IXamlType type, IXamlType other)
        {
            if (type.IsValueType || other.IsValueType)
                return type.Equals(other);
            return type.IsAssignableFrom(other);
        }

        public static IXamlType ThisOrFirstParameter(this IXamlMethod method) =>
            method.IsStatic ? method.Parameters[0] : method.DeclaringType;

        public static IReadOnlyList<IXamlType> ParametersWithThis(this IXamlMethod method)
        {
            if (method.IsStatic)
                return method.Parameters;
            var lst = method.Parameters.ToList();
            lst.Insert(0, method.DeclaringType);
            return lst;
        }
    }
    
}
