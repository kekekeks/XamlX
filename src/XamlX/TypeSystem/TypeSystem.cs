using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;

namespace XamlX.TypeSystem
{
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXType : IEquatable<IXamlXType>
    {
        object Id { get; }
        string Name { get; }
        string Namespace { get; }
        string FullName { get; }
        IXamlXAssembly Assembly { get; }
        IReadOnlyList<IXamlXProperty> Properties { get; }
        IReadOnlyList<IXamlXEventInfo> Events { get; }
        IReadOnlyList<IXamlXField> Fields { get; }
        IReadOnlyList<IXamlXMethod> Methods { get; }
        IReadOnlyList<IXamlXConstructor> Constructors { get; }
        IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; }
        IReadOnlyList<IXamlXType> GenericArguments { get; }
        bool IsAssignableFrom(IXamlXType type);
        IXamlXType MakeGenericType(IReadOnlyList<IXamlXType> typeArguments);
        IXamlXType GenericTypeDefinition { get; }
        bool IsArray { get; }
        IXamlXType ArrayElementType { get; }
        IXamlXType MakeArrayType(int dimensions);
        IXamlXType BaseType { get; }
        bool IsValueType { get; }
        bool IsEnum { get; }
        IReadOnlyList<IXamlXType> Interfaces { get; }
        bool IsInterface { get; }
        IXamlXType GetEnumUnderlyingType();
        IReadOnlyList<IXamlXType> GenericParameters { get; }
        int GetHashCode();
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXMethod : IEquatable<IXamlXMethod>, IXamlXMember
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlXType ReturnType { get; }
        IReadOnlyList<IXamlXType> Parameters { get; }
        IXamlXType DeclaringType { get; }
        IXamlXMethod MakeGenericMethod(IReadOnlyList<IXamlXType> typeArguments);
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXCustomEmitMethod : IXamlXMethod
    {
        void EmitCall(IXamlXEmitter emitter);
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXMember
    {
        string Name { get; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXConstructor : IEquatable<IXamlXConstructor>
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IReadOnlyList<IXamlXType> Parameters { get; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXProperty : IEquatable<IXamlXProperty>, IXamlXMember
    {
        IXamlXType PropertyType { get; }
        IXamlXMethod Setter { get; }
        IXamlXMethod Getter { get; }
        IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; }
        IReadOnlyList<IXamlXType> IndexerParameters { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXEventInfo : IEquatable<IXamlXEventInfo>, IXamlXMember
    {
        IXamlXMethod Add { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXField : IEquatable<IXamlXField>, IXamlXMember
    {
        IXamlXType FieldType { get; }
        bool IsPublic { get; }
        bool IsStatic { get; }
        bool IsLiteral { get; }
        object GetLiteralValue();
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXAssembly : IEquatable<IXamlXAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; }
        IXamlXType FindType(string fullName);
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXCustomAttribute : IEquatable<IXamlXCustomAttribute>
    {
        IXamlXType Type { get; }
        List<object> Parameters { get; }
        Dictionary<string, object> Properties { get; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXTypeSystem
    {
        IReadOnlyList<IXamlXAssembly> Assemblies { get; }
        IXamlXAssembly FindAssembly(string substring);
        IXamlXType FindType(string name);
        IXamlXType FindType(string name, string assembly);
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXEmitter
    {
        IXamlXTypeSystem TypeSystem { get; }
        IXamlXEmitter Emit(OpCode code);
        IXamlXEmitter Emit(OpCode code, IXamlXField field);
        IXamlXEmitter Emit(OpCode code, IXamlXMethod method);
        IXamlXEmitter Emit(OpCode code, IXamlXConstructor ctor);
        IXamlXEmitter Emit(OpCode code, string arg);
        IXamlXEmitter Emit(OpCode code, int arg);
        IXamlXEmitter Emit(OpCode code, long arg);
        IXamlXEmitter Emit(OpCode code, IXamlXType type);
        IXamlXEmitter Emit(OpCode code, float arg);
        IXamlXEmitter Emit(OpCode code, double arg);
        IXamlXLocal DefineLocal(IXamlXType type);
        IXamlXLabel DefineLabel();
        IXamlXEmitter MarkLabel(IXamlXLabel label);
        IXamlXEmitter Emit(OpCode code, IXamlXLabel label);
        IXamlXEmitter Emit(OpCode code, IXamlXLocal local);
        void InsertSequencePoint(IFileSource file, int line, int position);
        XamlXLocalsPool LocalsPool { get; }
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
    interface IXamlXLocal
    {
        
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXLabel
    {
        
    }

    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXMethodBuilder : IXamlXMethod
    {
        IXamlXEmitter Generator { get; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXConstructorBuilder : IXamlXConstructor
    {
        IXamlXEmitter Generator { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXTypeBuilder : IXamlXType
    {
        IXamlXField DefineField(IXamlXType type, string name, bool isPublic, bool isStatic);
        void AddInterfaceImplementation(IXamlXType type);

        IXamlXMethodBuilder DefineMethod(IXamlXType returnType, IEnumerable<IXamlXType> args, string name, bool isPublic, bool isStatic,
            bool isInterfaceImpl, IXamlXMethod overrideMethod = null);

        IXamlXProperty DefineProperty(IXamlXType propertyType, string name, IXamlXMethod setter, IXamlXMethod getter);
        IXamlXConstructorBuilder DefineConstructor(bool isStatic, params IXamlXType[] args);
        IXamlXType CreateType();
        IXamlXTypeBuilder DefineSubType(IXamlXType baseType, string name, bool isPublic);
        void DefineGenericParameters(IReadOnlyList<KeyValuePair<string, XamlXGenericParameterConstraint>> names);
    }


#if !XAMLIL_INTERNAL
    public
#endif
    struct XamlXGenericParameterConstraint
    {
        public bool IsClass { get; set; }
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXPseudoType : IXamlXType
    {
        public XamlXPseudoType(string name)
        {
            Name = name;
        }
        public bool Equals(IXamlXType other) => other == this;

        public object Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public string Namespace { get; } = "";
        public string FullName => Name;
        public IXamlXAssembly Assembly { get; } = null;
        public IReadOnlyList<IXamlXProperty> Properties { get; } = new IXamlXProperty[0];
        public IReadOnlyList<IXamlXEventInfo> Events { get; } = new IXamlXEventInfo[0];
        public IReadOnlyList<IXamlXField> Fields { get; } = new List<IXamlXField>();
        public IReadOnlyList<IXamlXMethod> Methods { get; } = new IXamlXMethod[0];
        public IReadOnlyList<IXamlXConstructor> Constructors { get; } = new IXamlXConstructor[0];
        public IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; } = new IXamlXCustomAttribute[0];
        public IReadOnlyList<IXamlXType> GenericArguments { get; } = new IXamlXType[0];
        public IXamlXType MakeArrayType(int dimensions) => throw new NullReferenceException();

        public IXamlXType BaseType { get; }
        public bool IsValueType { get; } = false;
        public bool IsEnum { get; } = false;
        public IReadOnlyList<IXamlXType> Interfaces { get; } = new IXamlXType[0];
        public bool IsInterface => false;
        public IXamlXType GetEnumUnderlyingType() => throw new InvalidOperationException();
        public IReadOnlyList<IXamlXType> GenericParameters { get; } = null;

        public bool IsAssignableFrom(IXamlXType type) => type == this;

        public IXamlXType MakeGenericType(IReadOnlyList<IXamlXType> typeArguments)
        {
            throw new NotSupportedException();
        }

        public IXamlXType GenericTypeDefinition => null;
        public bool IsArray { get; }
        public IXamlXType ArrayElementType { get; }
        public static XamlXPseudoType Null { get; } = new XamlXPseudoType("{x:Null}");
        public static XamlXPseudoType Unknown { get; } = new XamlXPseudoType("{Unknown type}");

        public static XamlXPseudoType Unresolved(string message) =>
            new XamlXPseudoType($"{{Unresolved type: '{message}'}}");
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    static class XamlXTypeSystemExtensions
    {
        public static string GetFqn(this IXamlXType type) => $"{type.Assembly?.Name}:{type.Namespace}.{type.Name}";

        public static string GetFullName(this IXamlXType type)
        {
            var name = type.Name;
            if (type.Namespace != null)
                name = type.Namespace + "." + name;
            if (type.Assembly != null)
                name += "," + type.Assembly.Name;
            return name;
        }
        
        public static IXamlXType GetType(this IXamlXTypeSystem sys, string type)
        {
            var f = sys.FindType(type);
            if (f == null)
                throw new XamlXTypeSystemException("Unable to resolve type " + type);
            return f;
        }
        
        public static IEnumerable<IXamlXMethod> FindMethods(this IXamlXType type, Func<IXamlXMethod, bool> criteria)
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

        public static IXamlXMethod FindMethod(this IXamlXType type, Func<IXamlXMethod, bool> criteria)
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
        
        public static IXamlXMethod FindMethod(this IXamlXType type, string name, IXamlXType returnType, 
            bool allowDowncast, params IXamlXType[] args)
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
        
        public static IXamlXConstructor FindConstructor(this IXamlXType type, List<IXamlXType> args = null)
        {
            if(args == null)
                args = new List<IXamlXType>();
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

        public static bool IsNullable(this IXamlXType type)
        {
            var def = type.GenericTypeDefinition;
            if (def == null) return false;
            return def.Namespace == "System" && def.Name == "Nullable`1";
        }

        public static bool IsNullableOf(this IXamlXType type, IXamlXType vtype)
        {
            return type.IsNullable() && type.GenericArguments[0].Equals(vtype);
        }

        public static IXamlXEmitter EmitCall(this IXamlXEmitter emitter, IXamlXMethod method, bool swallowResult = false)
        {
            if(method is IXamlXCustomEmitMethod custom)
                custom.EmitCall(emitter);
            else
                emitter.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);

            if (swallowResult && !(method.ReturnType.Namespace == "System" && method.ReturnType.Name == "Void"))
                emitter.Pop();
            return emitter;
        }

        public static IXamlXType MakeGenericType(this IXamlXType type, params IXamlXType[] typeArguments)
            => type.MakeGenericType(typeArguments);

        public static IEnumerable<IXamlXType> GetAllInterfaces(this IXamlXType type)
        {
            foreach (var i in type.Interfaces)
                yield return i;
            if(type.BaseType!=null)
                foreach (var i in type.BaseType.GetAllInterfaces())
                    yield return i;
        }

        public static IEnumerable<IXamlXProperty> GetAllProperties(this IXamlXType t)
        {
            foreach (var p in t.Properties)
                yield return p;
            if(t.BaseType!=null)
                foreach (var p in t.BaseType.GetAllProperties())
                    yield return p;
        }

        public static IEnumerable<IXamlXField> GetAllFields(this IXamlXType t)
        {
            foreach (var p in t.Fields)
                yield return p;
            if (t.BaseType != null)
                foreach (var p in t.BaseType.GetAllFields())
                    yield return p;
        }

        public static IEnumerable<IXamlXEventInfo> GetAllEvents(this IXamlXType t)
        {
            foreach (var p in t.Events)
                yield return p;
            if(t.BaseType!=null)
                foreach (var p in t.BaseType.GetAllEvents())
                    yield return p;
        }

        public static bool IsDirectlyAssignableFrom(this IXamlXType type, IXamlXType other)
        {
            if (type.IsValueType || other.IsValueType)
                return type.Equals(other);
            return type.IsAssignableFrom(other);
        }

        public static IXamlXType ThisOrFirstParameter(this IXamlXMethod method) =>
            method.IsStatic ? method.Parameters[0] : method.DeclaringType;

        public static IReadOnlyList<IXamlXType> ParametersWithThis(this IXamlXMethod method)
        {
            if (method.IsStatic)
                return method.Parameters;
            var lst = method.Parameters.ToList();
            lst.Insert(0, method.DeclaringType);
            return lst;
        }
        
        public static IXamlXEmitter DebugHatch(this IXamlXEmitter emitter, string message)
        {
            #if DEBUG
            var debug = emitter.TypeSystem.GetType("XamlX.XamlXDebugHatch").FindMethod(m => m.Name == "Debug");
            emitter.Emit(OpCodes.Ldstr, message);
            emitter.Emit(OpCodes.Call, debug);
            #endif
            return emitter;
        }
    }
    
}
