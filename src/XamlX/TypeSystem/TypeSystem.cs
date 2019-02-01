using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace XamlX.TypeSystem
{
    public interface IXamlType : IEquatable<IXamlType>
    {
        object Id { get; }
        string Name { get; }
        string Namespace { get; }
        IXamlAssembly Assembly { get; }
        IReadOnlyList<IXamlProperty> Properties { get; }
        IReadOnlyList<IXamlMethod> Methods { get; }
        IReadOnlyList<IXamlConstructor> Constructors { get; }
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
        bool IsAssignableFrom(IXamlType type);
        IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments);
        IXamlType BaseType { get; }
    }

    public interface IXamlMethod : IEquatable<IXamlMethod>
    {
        string Name { get; }
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlType ReturnType { get; }
        IReadOnlyList<IXamlType> Parameters { get; }
    }

    public interface IXamlConstructor : IEquatable<IXamlConstructor>
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IReadOnlyList<IXamlType> Parameters { get; }
    }
    
    public interface IXamlProperty : IEquatable<IXamlProperty>
    {
        string Name { get; }
        IXamlType PropertyType { get; }
        IXamlMethod Setter { get; }
        IXamlMethod Getter { get; }
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
    }

    public interface IXamlAssembly : IEquatable<IXamlAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlType> Types { get; }
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
    }
    
    public interface IXamlILEmitter
    {
        IXamlILEmitter Emit(OpCode code);
        IXamlILEmitter Emit(OpCode code, IXamlMethod method);
        IXamlILEmitter Emit(OpCode code, IXamlConstructor ctor);
        IXamlILEmitter Emit(OpCode code, string arg);
        IXamlILEmitter Emit(OpCode code, IXamlType type);
    }

    public interface IXamlXClosure : IXamlXCodeGen
    {
        
    }
    
    public interface IXamlXCodeGen
    {
        IXamlILEmitter Generator { get; }
        void EmitClosure(IEnumerable<IXamlType> fields);
    }

    public class XamlXNullType : IXamlType
    {
        public bool Equals(IXamlType other) => other == this;

        public object Id { get; } = Guid.NewGuid();
        public string Name { get; } = "{x:Null}";
        public string Namespace { get; } = "";
        public IXamlAssembly Assembly { get; } = null;
        public IReadOnlyList<IXamlProperty> Properties { get; } = new IXamlProperty[0];
        public IReadOnlyList<IXamlMethod> Methods { get; } = new IXamlMethod[0];
        public IReadOnlyList<IXamlConstructor> Constructors { get; } = new IXamlConstructor[0];
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; } = new IXamlCustomAttribute[0];
        public IXamlType BaseType { get; }
        public bool IsAssignableFrom(IXamlType type) => type == this;

        public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments)
        {
            throw new NotSupportedException();
        }

        XamlXNullType()
        {
            
        }
        public static XamlXNullType Instance { get; } = new XamlXNullType();
    }
    
    public static class XamlTypeSystemExtensions
    {
        public static string GetFqn(this IXamlType type) => $"{type.Assembly?.Name}:{type.Namespace}.{type.Name}";

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
        
        public static IXamlConstructor FindConstructor(this IXamlType type, List<IXamlType> args)
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