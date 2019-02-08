using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;

namespace XamlIl.TypeSystem
{
    public interface IXamlIlType : IEquatable<IXamlIlType>
    {
        object Id { get; }
        string Name { get; }
        string Namespace { get; }
        IXamlIlAssembly Assembly { get; }
        IReadOnlyList<IXamlIlProperty> Properties { get; }
        IReadOnlyList<IXamlIlField> Fields { get; }
        IReadOnlyList<IXamlIlMethod> Methods { get; }
        IReadOnlyList<IXamlIlConstructor> Constructors { get; }
        IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; }
        IReadOnlyList<IXamlIlType> GenericArguments { get; }
        bool IsAssignableFrom(IXamlIlType type);
        IXamlIlType MakeGenericType(IReadOnlyList<IXamlIlType> typeArguments);
        IXamlIlType GenericTypeDefinition { get; }
        IXamlIlType BaseType { get; }
        bool IsValueType { get; }
        bool IsEnum { get; }
        IReadOnlyList<IXamlIlType> Interfaces { get; }
        IXamlIlType GetEnumUnderlyingType();
        
    }

    public interface IXamlIlMethod : IEquatable<IXamlIlMethod>, IXamlIlMember
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlIlType ReturnType { get; }
        IReadOnlyList<IXamlIlType> Parameters { get; }
        IXamlIlType DeclaringType { get; }
    }

    public interface IXamlIlMember
    {
        string Name { get; }
    }
    
    public interface IXamlIlConstructor : IEquatable<IXamlIlConstructor>
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IReadOnlyList<IXamlIlType> Parameters { get; }
    }
    
    public interface IXamlIlProperty : IEquatable<IXamlIlProperty>, IXamlIlMember
    {
        IXamlIlType PropertyType { get; }
        IXamlIlMethod Setter { get; }
        IXamlIlMethod Getter { get; }
        IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; }
    }

    public interface IXamlIlField : IEquatable<IXamlIlField>, IXamlIlMember
    {
        IXamlIlType FieldType { get; }
        bool IsPublic { get; }
        bool IsStatic { get; }
        bool IsLiteral { get; }
        object GetLiteralValue();
    }

    public interface IXamlIlAssembly : IEquatable<IXamlIlAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlIlType> Types { get; }
        IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; }
        IXamlIlType FindType(string fullName);
    }

    public interface IXamlIlCustomAttribute : IEquatable<IXamlIlCustomAttribute>
    {
        IXamlIlType Type { get; }
        List<object> Parameters { get; }
        Dictionary<string, object> Properties { get; }
    }
    
    public interface IXamlIlTypeSystem
    {
        IReadOnlyList<IXamlIlAssembly> Assemblies { get; }
        IXamlIlAssembly FindAssembly(string substring);
        IXamlIlType FindType(string name);
    }
    
    public interface IXamlIlEmitter
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
    }

    public interface IXamlIlLocal
    {
        
    }
    
    public interface IXamlIlLabel
    {
        
    }

    public interface IXamlIlClosure : IXamlIlCodeGen
    {
        
    }
    
    public interface IXamlIlCodeGen
    {
        IXamlIlEmitter Generator { get; }
        void EmitClosure(IEnumerable<IXamlIlType> fields);
    }

    public interface IXamlIlMethodBuilder : IXamlIlMethod, IXamlIlCodeGen
    {
        IXamlIlEmitter Generator { get; }
    }
    
    public interface IXamlIlConstructorBuilder : IXamlIlConstructor
    {
        IXamlIlEmitter Generator { get; }
    }

    public interface IXamlIlTypeBuilder : IXamlIlType
    {
        IXamlIlField DefineField(IXamlIlType type, string name, bool isPublic, bool isStatic);
        void AddInterfaceImplementation(IXamlIlType type);

        IXamlIlMethodBuilder DefineMethod(IXamlIlType returnType, IEnumerable<IXamlIlType> args, string name, bool isPublic, bool isStatic,
            bool isInterfaceImpl, IXamlIlMethod overrideMethod = null);

        IXamlIlProperty DefineProperty(IXamlIlType propertyType, string name, IXamlIlMethod setter, IXamlIlMethod getter);
        IXamlIlConstructorBuilder DefineConstructor(params IXamlIlType[] args);
        IXamlIlType CreateType();
        IXamlIlTypeBuilder DefineSubType(IXamlIlType baseType, string name, bool isPublic);
    }
    
    
    public class XamlIlPseudoType : IXamlIlType
    {
        public XamlIlPseudoType(string name)
        {
            Name = name;
        }
        public bool Equals(IXamlIlType other) => other == this;

        public object Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public string Namespace { get; } = "";
        public IXamlIlAssembly Assembly { get; } = null;
        public IReadOnlyList<IXamlIlProperty> Properties { get; } = new IXamlIlProperty[0];
        public IReadOnlyList<IXamlIlField> Fields { get; } = new List<IXamlIlField>();
        public IReadOnlyList<IXamlIlMethod> Methods { get; } = new IXamlIlMethod[0];
        public IReadOnlyList<IXamlIlConstructor> Constructors { get; } = new IXamlIlConstructor[0];
        public IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; } = new IXamlIlCustomAttribute[0];
        public IReadOnlyList<IXamlIlType> GenericArguments { get; } = new IXamlIlType[0];
        public IXamlIlType BaseType { get; }
        public bool IsValueType { get; } = false;
        public bool IsEnum { get; } = false;
        public IReadOnlyList<IXamlIlType> Interfaces { get; } = new IXamlIlType[0];
        public IXamlIlType GetEnumUnderlyingType() => throw new InvalidOperationException();

        public bool IsAssignableFrom(IXamlIlType type) => type == this;

        public IXamlIlType MakeGenericType(IReadOnlyList<IXamlIlType> typeArguments)
        {
            throw new NotSupportedException();
        }

        public IXamlIlType GenericTypeDefinition => null;
        public static XamlIlPseudoType Null { get; } = new XamlIlPseudoType("{x:Null}");
        public static XamlIlPseudoType Unknown { get; } = new XamlIlPseudoType("{Unknown type}");
    }
    
    public static class XamlIlTypeSystemExtensions
    {
        public static string GetFqn(this IXamlIlType type) => $"{type.Assembly?.Name}:{type.Namespace}.{type.Name}";

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
        
        public static IXamlIlConstructor FindConstructor(this IXamlIlType type, List<IXamlIlType> args)
        {
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
            emitter.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);
            if (swallowResult && !(method.ReturnType.Namespace == "System" && method.ReturnType.Name == "Void"))
                emitter.Emit(OpCodes.Pop);
            return emitter;
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