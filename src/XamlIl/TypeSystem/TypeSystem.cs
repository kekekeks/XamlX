using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace XamlIl.TypeSystem
{
    public interface IXamlIlType : IEquatable<IXamlIlType>
    {
        object Id { get; }
        string Name { get; }
        string Namespace { get; }
        IXamlIlAssembly Assembly { get; }
        IReadOnlyList<IXamlIlProperty> Properties { get; }
        IReadOnlyList<IXamlIlMethod> Methods { get; }
        IReadOnlyList<IXamlIlConstructor> Constructors { get; }
        IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; }
        bool IsAssignableFrom(IXamlIlType type);
        IXamlIlType MakeGenericType(IReadOnlyList<IXamlIlType> typeArguments);
        IXamlIlType BaseType { get; }
    }

    public interface IXamlIlMethod : IEquatable<IXamlIlMethod>
    {
        string Name { get; }
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlIlType ReturnType { get; }
        IReadOnlyList<IXamlIlType> Parameters { get; }
    }

    public interface IXamlIlConstructor : IEquatable<IXamlIlConstructor>
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IReadOnlyList<IXamlIlType> Parameters { get; }
    }
    
    public interface IXamlIlProperty : IEquatable<IXamlIlProperty>
    {
        string Name { get; }
        IXamlIlType PropertyType { get; }
        IXamlIlMethod Setter { get; }
        IXamlIlMethod Getter { get; }
        IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; }
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
        IXamlIlEmitter Emit(OpCode code);
        IXamlIlEmitter Emit(OpCode code, IXamlIlMethod method);
        IXamlIlEmitter Emit(OpCode code, IXamlIlConstructor ctor);
        IXamlIlEmitter Emit(OpCode code, string arg);
        IXamlIlEmitter Emit(OpCode code, IXamlIlType type);
    }

    public interface IXamlIlClosure : IXamlIlCodeGen
    {
        
    }
    
    public interface IXamlIlCodeGen
    {
        IXamlIlEmitter Generator { get; }
        void EmitClosure(IEnumerable<IXamlIlType> fields);
    }
    
    public static class XamlIlTypeSystemExtensions
    {
        public static string GetFqn(this IXamlIlType type) => $"{type.Assembly.Name}:{type.Namespace}.{type.Name}";

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
    }
    
}