using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;

namespace XamlX.TypeSystem
{
    public interface IXamlType : IEquatable<IXamlType>
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
    }

    public interface IXamlMethod : IEquatable<IXamlMethod>, IXamlMember
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlType ReturnType { get; }
        IReadOnlyList<IXamlType> Parameters { get; }
        IXamlType DeclaringType { get; }
    }

    public interface IXamlMember
    {
        string Name { get; }
    }
    
    public interface IXamlConstructor : IEquatable<IXamlConstructor>
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IReadOnlyList<IXamlType> Parameters { get; }
    }
    
    public interface IXamlProperty : IEquatable<IXamlProperty>, IXamlMember
    {
        IXamlType PropertyType { get; }
        IXamlMethod Setter { get; }
        IXamlMethod Getter { get; }
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
    }

    public interface IXamlEventInfo : IEquatable<IXamlEventInfo>, IXamlMember
    {
        IXamlMethod Add { get; }
    }

    public interface IXamlField : IEquatable<IXamlField>, IXamlMember
    {
        IXamlType FieldType { get; }
        bool IsPublic { get; }
        bool IsStatic { get; }
        bool IsLiteral { get; }
        object GetLiteralValue();
    }

    public interface IXamlAssembly : IEquatable<IXamlAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
        IXamlType FindType(string fullName);
    }

    public interface IXamlCustomAttribute : IEquatable<IXamlCustomAttribute>
    {
        IXamlType Type { get; }
        List<object> Parameters { get; }
        Dictionary<string, object> Properties { get; }
    }
    
    public interface IXamlTypeSystem
    {
        IReadOnlyList<IXamlAssembly> Assemblies { get; }
        IXamlAssembly FindAssembly(string substring);
        IXamlType FindType(string name);
        IXamlType FindType(string name, string assembly);
    }
    
    public interface IXamlILEmitter
    {
        IXamlTypeSystem TypeSystem { get; }
        IXamlILEmitter Emit(OpCode code);
        IXamlILEmitter Emit(OpCode code, IXamlField field);
        IXamlILEmitter Emit(OpCode code, IXamlMethod method);
        IXamlILEmitter Emit(OpCode code, IXamlConstructor ctor);
        IXamlILEmitter Emit(OpCode code, string arg);
        IXamlILEmitter Emit(OpCode code, int arg);
        IXamlILEmitter Emit(OpCode code, long arg);
        IXamlILEmitter Emit(OpCode code, IXamlType type);
        IXamlILEmitter Emit(OpCode code, float arg);
        IXamlILEmitter Emit(OpCode code, double arg);
        IXamlLocal DefineLocal(IXamlType type);
        IXamlLabel DefineLabel();
        IXamlILEmitter MarkLabel(IXamlLabel label);
        IXamlILEmitter Emit(OpCode code, IXamlLabel label);
        IXamlILEmitter Emit(OpCode code, IXamlLocal local);
        void InsertSequencePoint(IFileSource file, int line, int position);
    }

    public interface IFileSource
    {
        string FilePath { get; }
        byte[] FileContents { get; }
    }

    public interface IXamlLocal
    {
        
    }
    
    public interface IXamlLabel
    {
        
    }

    
    public interface IXamlMethodBuilder : IXamlMethod
    {
        IXamlILEmitter Generator { get; }
    }
    
    public interface IXamlConstructorBuilder : IXamlConstructor
    {
        IXamlILEmitter Generator { get; }
    }

    public interface IXamlTypeBuilder : IXamlType
    {
        IXamlField DefineField(IXamlType type, string name, bool isPublic, bool isStatic);
        void AddInterfaceImplementation(IXamlType type);

        IXamlMethodBuilder DefineMethod(IXamlType returnType, IEnumerable<IXamlType> args, string name, bool isPublic, bool isStatic,
            bool isInterfaceImpl, IXamlMethod overrideMethod = null);

        IXamlProperty DefineProperty(IXamlType propertyType, string name, IXamlMethod setter, IXamlMethod getter);
        IXamlConstructorBuilder DefineConstructor(bool isStatic, params IXamlType[] args);
        IXamlType CreateType();
        IXamlTypeBuilder DefineSubType(IXamlType baseType, string name, bool isPublic);
        void DefineGenericParameters(IReadOnlyList<KeyValuePair<string, XamlGenericParameterConstraint>> names);
    }


    public struct XamlGenericParameterConstraint
    {
        public bool IsClass { get; set; }
    }
    
    public class XamlPseudoType : IXamlType
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
    }
    
    public static class XamlTypeSystemExtensions
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

        public static IXamlILEmitter EmitCall(this IXamlILEmitter emitter, IXamlMethod method, bool swallowResult = false)
        {
            emitter.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);
            if (swallowResult && !(method.ReturnType.Namespace == "System" && method.ReturnType.Name == "Void"))
                emitter.Emit(OpCodes.Pop);
            return emitter;
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
        
        public static IEnumerable<IXamlEventInfo> GetAllEvents(this IXamlType t)
        {
            foreach (var p in t.Events)
                yield return p;
            if(t.BaseType!=null)
                foreach (var p in t.BaseType.GetAllEvents())
                    yield return p;
        }
        
        public static IXamlILEmitter DebugHatch(this IXamlILEmitter emitter, string message)
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
