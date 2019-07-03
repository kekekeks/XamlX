using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;

namespace XamlIl.TypeSystem
{
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlType : IEquatable<IXamlIlType>
    {
        object Id { get; }
        string Name { get; }
        string Namespace { get; }
        string FullName { get; }
        IXamlIlAssembly Assembly { get; }
        IReadOnlyList<IXamlIlProperty> Properties { get; }
        IReadOnlyList<IXamlIlEventInfo> Events { get; }
        IReadOnlyList<IXamlIlField> Fields { get; }
        IReadOnlyList<IXamlIlMethod> Methods { get; }
        IReadOnlyList<IXamlIlConstructor> Constructors { get; }
        IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; }
        IReadOnlyList<IXamlIlType> GenericArguments { get; }
        bool IsAssignableFrom(IXamlIlType type);
        IXamlIlType MakeGenericType(IReadOnlyList<IXamlIlType> typeArguments);
        IXamlIlType GenericTypeDefinition { get; }
        bool IsArray { get; }
        IXamlIlType ArrayElementType { get; }
        IXamlIlType MakeArrayType(int dimensions);
        IXamlIlType BaseType { get; }
        bool IsValueType { get; }
        bool IsEnum { get; }
        IReadOnlyList<IXamlIlType> Interfaces { get; }
        bool IsInterface { get; }
        IXamlIlType GetEnumUnderlyingType();
        IReadOnlyList<IXamlIlType> GenericParameters { get; }
        int GetHashCode();
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlMethod : IEquatable<IXamlIlMethod>, IXamlIlMember
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlIlType ReturnType { get; }
        IReadOnlyList<IXamlIlType> Parameters { get; }
        IXamlIlType DeclaringType { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlCustomEmitMethod : IXamlIlMethod
    {
        void EmitCall(IXamlIlEmitter emitter);
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlMember
    {
        string Name { get; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlConstructor : IEquatable<IXamlIlConstructor>
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IReadOnlyList<IXamlIlType> Parameters { get; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlProperty : IEquatable<IXamlIlProperty>, IXamlIlMember
    {
        IXamlIlType PropertyType { get; }
        IXamlIlMethod Setter { get; }
        IXamlIlMethod Getter { get; }
        IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlEventInfo : IEquatable<IXamlIlEventInfo>, IXamlIlMember
    {
        IXamlIlMethod Add { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlField : IEquatable<IXamlIlField>, IXamlIlMember
    {
        IXamlIlType FieldType { get; }
        bool IsPublic { get; }
        bool IsStatic { get; }
        bool IsLiteral { get; }
        object GetLiteralValue();
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlAssembly : IEquatable<IXamlIlAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; }
        IXamlIlType FindType(string fullName);
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlCustomAttribute : IEquatable<IXamlIlCustomAttribute>
    {
        IXamlIlType Type { get; }
        List<object> Parameters { get; }
        Dictionary<string, object> Properties { get; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlTypeSystem
    {
        IReadOnlyList<IXamlIlAssembly> Assemblies { get; }
        IXamlIlAssembly FindAssembly(string substring);
        IXamlIlType FindType(string name);
        IXamlIlType FindType(string name, string assembly);
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlEmitter
    {
        IXamlIlTypeSystem TypeSystem { get; }
        IXamlIlEmitter Emit(OpCode code);
        IXamlIlEmitter Emit(OpCode code, IXamlIlField field);
        IXamlIlEmitter Emit(OpCode code, IXamlIlMethod method);
        IXamlIlEmitter Emit(OpCode code, IXamlIlConstructor ctor);
        IXamlIlEmitter Emit(OpCode code, string arg);
        IXamlIlEmitter Emit(OpCode code, int arg);
        IXamlIlEmitter Emit(OpCode code, long arg);
        IXamlIlEmitter Emit(OpCode code, IXamlIlType type);
        IXamlIlEmitter Emit(OpCode code, float arg);
        IXamlIlEmitter Emit(OpCode code, double arg);
        IXamlIlLocal DefineLocal(IXamlIlType type);
        IXamlIlLabel DefineLabel();
        IXamlIlEmitter MarkLabel(IXamlIlLabel label);
        IXamlIlEmitter Emit(OpCode code, IXamlIlLabel label);
        IXamlIlEmitter Emit(OpCode code, IXamlIlLocal local);
        void InsertSequencePoint(IFileSource file, int line, int position);
        XamlIlLocalsPool LocalsPool { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IFileSource
    {
        string FilePath { get; }
        byte[] FileContents { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlLocal
    {
        
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlLabel
    {
        
    }

    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlMethodBuilder : IXamlIlMethod
    {
        IXamlIlEmitter Generator { get; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlConstructorBuilder : IXamlIlConstructor
    {
        IXamlIlEmitter Generator { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlTypeBuilder : IXamlIlType
    {
        IXamlIlField DefineField(IXamlIlType type, string name, bool isPublic, bool isStatic);
        void AddInterfaceImplementation(IXamlIlType type);

        IXamlIlMethodBuilder DefineMethod(IXamlIlType returnType, IEnumerable<IXamlIlType> args, string name, bool isPublic, bool isStatic,
            bool isInterfaceImpl, IXamlIlMethod overrideMethod = null);

        IXamlIlProperty DefineProperty(IXamlIlType propertyType, string name, IXamlIlMethod setter, IXamlIlMethod getter);
        IXamlIlConstructorBuilder DefineConstructor(bool isStatic, params IXamlIlType[] args);
        IXamlIlType CreateType();
        IXamlIlTypeBuilder DefineSubType(IXamlIlType baseType, string name, bool isPublic);
        void DefineGenericParameters(IReadOnlyList<KeyValuePair<string, XamlIlGenericParameterConstraint>> names);
    }


#if !XAMLIL_INTERNAL
    public
#endif
    struct XamlIlGenericParameterConstraint
    {
        public bool IsClass { get; set; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlPseudoType : IXamlIlType
    {
        public XamlIlPseudoType(string name)
        {
            Name = name;
        }
        public bool Equals(IXamlIlType other) => other == this;

        public object Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public string Namespace { get; } = "";
        public string FullName => Name;
        public IXamlIlAssembly Assembly { get; } = null;
        public IReadOnlyList<IXamlIlProperty> Properties { get; } = new IXamlIlProperty[0];
        public IReadOnlyList<IXamlIlEventInfo> Events { get; } = new IXamlIlEventInfo[0];
        public IReadOnlyList<IXamlIlField> Fields { get; } = new List<IXamlIlField>();
        public IReadOnlyList<IXamlIlMethod> Methods { get; } = new IXamlIlMethod[0];
        public IReadOnlyList<IXamlIlConstructor> Constructors { get; } = new IXamlIlConstructor[0];
        public IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; } = new IXamlIlCustomAttribute[0];
        public IReadOnlyList<IXamlIlType> GenericArguments { get; } = new IXamlIlType[0];
        public IXamlIlType MakeArrayType(int dimensions) => throw new NullReferenceException();

        public IXamlIlType BaseType { get; }
        public bool IsValueType { get; } = false;
        public bool IsEnum { get; } = false;
        public IReadOnlyList<IXamlIlType> Interfaces { get; } = new IXamlIlType[0];
        public bool IsInterface => false;
        public IXamlIlType GetEnumUnderlyingType() => throw new InvalidOperationException();
        public IReadOnlyList<IXamlIlType> GenericParameters { get; } = null;

        public bool IsAssignableFrom(IXamlIlType type) => type == this;

        public IXamlIlType MakeGenericType(IReadOnlyList<IXamlIlType> typeArguments)
        {
            throw new NotSupportedException();
        }

        public IXamlIlType GenericTypeDefinition => null;
        public bool IsArray { get; }
        public IXamlIlType ArrayElementType { get; }
        public static XamlIlPseudoType Null { get; } = new XamlIlPseudoType("{x:Null}");
        public static XamlIlPseudoType Unknown { get; } = new XamlIlPseudoType("{Unknown type}");

        public static XamlIlPseudoType Unresolved(string message) =>
            new XamlIlPseudoType($"{{Unresolved type: '{message}'}}");
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    class FindMethodMethodSignature
    {
        public string Name { get; set; }
        public IXamlIlType ReturnType { get; set; }
        public bool IsStatic { get; set; }
        public bool IsExactMatch { get; set; } = true;
        public bool DeclaringOnly { get; set; } = false;
        public IReadOnlyList<IXamlIlType> Parameters { get; set; }

        public FindMethodMethodSignature(string name, IXamlIlType returnType, params IXamlIlType[] parameters)
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
    
#if !XAMLIL_INTERNAL
    public
#endif
    static class XamlIlTypeSystemExtensions
    {
        public static string GetFqn(this IXamlIlType type) => $"{type.Assembly?.Name}:{type.Namespace}.{type.Name}";

        public static string GetFullName(this IXamlIlType type)
        {
            var name = type.Name;
            if (type.Namespace != null)
                name = type.Namespace + "." + name;
            if (type.Assembly != null)
                name += "," + type.Assembly.Name;
            return name;
        }
        
        public static IXamlIlType GetType(this IXamlIlTypeSystem sys, string type)
        {
            var f = sys.FindType(type);
            if (f == null)
                throw new XamlIlTypeSystemException("Unable to resolve type " + type);
            return f;
        }
        
        public static IEnumerable<IXamlIlMethod> FindMethods(this IXamlIlType type, Func<IXamlIlMethod, bool> criteria)
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

        public static IXamlIlMethod FindMethod(this IXamlIlType type, Func<IXamlIlMethod, bool> criteria)
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
        
        public static IXamlIlMethod FindMethod(this IXamlIlType type, string name, IXamlIlType returnType, 
            bool allowDowncast, params IXamlIlType[] args)
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

        public static IXamlIlMethod GetMethod(this IXamlIlType type, FindMethodMethodSignature signature)
        {
            var found = FindMethod(type, signature);
            if (found == null)
                throw new XamlIlTypeSystemException($"Method with signature {signature} is not found on type {type.GetFqn()}");
            return found;
        }
        public static IXamlIlMethod FindMethod(this IXamlIlType type, FindMethodMethodSignature signature)
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
        
        public static IXamlIlConstructor FindConstructor(this IXamlIlType type, List<IXamlIlType> args = null)
        {
            if(args == null)
                args = new List<IXamlIlType>();
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

        public static bool IsNullable(this IXamlIlType type)
        {
            var def = type.GenericTypeDefinition;
            if (def == null) return false;
            return def.Namespace == "System" && def.Name == "Nullable`1";
        }

        public static bool IsNullableOf(this IXamlIlType type, IXamlIlType vtype)
        {
            return type.IsNullable() && type.GenericArguments[0].Equals(vtype);
        }

        public static IXamlIlEmitter EmitCall(this IXamlIlEmitter emitter, IXamlIlMethod method, bool swallowResult = false)
        {
            if(method is IXamlIlCustomEmitMethod custom)
                custom.EmitCall(emitter);
            else
                emitter.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);

            if (swallowResult && !(method.ReturnType.Namespace == "System" && method.ReturnType.Name == "Void"))
                emitter.Pop();
            return emitter;
        }

        public static IXamlIlType MakeGenericType(this IXamlIlType type, params IXamlIlType[] typeArguments)
            => type.MakeGenericType(typeArguments);

        public static IEnumerable<IXamlIlType> GetAllInterfaces(this IXamlIlType type)
        {
            foreach (var i in type.Interfaces)
                yield return i;
            if(type.BaseType!=null)
                foreach (var i in type.BaseType.GetAllInterfaces())
                    yield return i;
        }

        public static IEnumerable<IXamlIlProperty> GetAllProperties(this IXamlIlType t)
        {
            foreach (var p in t.Properties)
                yield return p;
            if(t.BaseType!=null)
                foreach (var p in t.BaseType.GetAllProperties())
                    yield return p;
        }

        public static IEnumerable<IXamlIlField> GetAllFields(this IXamlIlType t)
        {
            foreach (var p in t.Fields)
                yield return p;
            if (t.BaseType != null)
                foreach (var p in t.BaseType.GetAllFields())
                    yield return p;
        }

        public static IEnumerable<IXamlIlEventInfo> GetAllEvents(this IXamlIlType t)
        {
            foreach (var p in t.Events)
                yield return p;
            if(t.BaseType!=null)
                foreach (var p in t.BaseType.GetAllEvents())
                    yield return p;
        }

        public static bool IsDirectlyAssignableFrom(this IXamlIlType type, IXamlIlType other)
        {
            if (type.IsValueType || other.IsValueType)
                return type.Equals(other);
            return type.IsAssignableFrom(other);
        }

        public static IXamlIlType ThisOrFirstParameter(this IXamlIlMethod method) =>
            method.IsStatic ? method.Parameters[0] : method.DeclaringType;

        public static IReadOnlyList<IXamlIlType> ParametersWithThis(this IXamlIlMethod method)
        {
            if (method.IsStatic)
                return method.Parameters;
            var lst = method.Parameters.ToList();
            lst.Insert(0, method.DeclaringType);
            return lst;
        }
        
        public static IXamlIlEmitter DebugHatch(this IXamlIlEmitter emitter, string message)
        {
            #if DEBUG
            var debug = emitter.TypeSystem.GetType("XamlIl.XamlIlDebugHatch").FindMethod(m => m.Name == "Debug");
            emitter.Emit(OpCodes.Ldstr, message);
            emitter.Emit(OpCodes.Call, debug);
            #endif
            return emitter;
        }
    }
    
}
