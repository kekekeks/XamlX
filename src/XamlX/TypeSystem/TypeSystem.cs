using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace XamlX.TypeSystem
{
    public interface IXamlXType : IEquatable<IXamlXType>
    {
        object Id { get; }
        string Name { get; }
        string Namespace { get; }
        IXamlXAssembly Assembly { get; }
        IReadOnlyList<IXamlXProperty> Properties { get; }
        IReadOnlyList<IXamlXMethod> Methods { get; }
        IReadOnlyList<IXamlXConstructor> Constructors { get; }
        IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; }
        bool IsAssignableFrom(IXamlXType type);
        IXamlXType MakeGenericType(IReadOnlyList<IXamlXType> typeArguments);
        IXamlXType BaseType { get; }
    }

    public interface IXamlXMethod : IEquatable<IXamlXMethod>
    {
        string Name { get; }
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlXType ReturnType { get; }
        IReadOnlyList<IXamlXType> Parameters { get; }
    }

    public interface IXamlXConstructor : IEquatable<IXamlXConstructor>
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IReadOnlyList<IXamlXType> Parameters { get; }
    }
    
    public interface IXamlXProperty : IEquatable<IXamlXProperty>
    {
        string Name { get; }
        IXamlXType PropertyType { get; }
        IXamlXMethod Setter { get; }
        IXamlXMethod Getter { get; }
        IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; }
    }

    public interface IXamlXAssembly : IEquatable<IXamlXAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlXType> Types { get; }
        IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; }
        IXamlXType FindType(string fullName);
    }

    public interface IXamlXCustomAttribute : IEquatable<IXamlXCustomAttribute>
    {
        IXamlXType Type { get; }
        List<object> Parameters { get; }
        Dictionary<string, object> Properties { get; }
    }
    
    public interface IXamlXTypeSystem
    {
        IReadOnlyList<IXamlXAssembly> Assemblies { get; }
        IXamlXAssembly FindAssembly(string substring);
        IXamlXType FindType(string name);
    }
    
    public interface IXamlXEmitter
    {
        IXamlXEmitter Emit(OpCode code);
        IXamlXEmitter Emit(OpCode code, IXamlXMethod method);
        IXamlXEmitter Emit(OpCode code, IXamlXConstructor ctor);
        IXamlXEmitter Emit(OpCode code, string arg);
        IXamlXEmitter Emit(OpCode code, IXamlXType type);
    }

    public interface IXamlXClosure : IXamlXCodeGen
    {
        
    }
    
    public interface IXamlXCodeGen
    {
        IXamlXEmitter Generator { get; }
        void EmitClosure(IEnumerable<IXamlXType> fields);
    }

    public class XamlXNullType : IXamlXType
    {
        public bool Equals(IXamlXType other) => other == this;

        public object Id { get; } = Guid.NewGuid();
        public string Name { get; } = "{x:Null}";
        public string Namespace { get; } = "";
        public IXamlXAssembly Assembly { get; } = null;
        public IReadOnlyList<IXamlXProperty> Properties { get; } = new IXamlXProperty[0];
        public IReadOnlyList<IXamlXMethod> Methods { get; } = new IXamlXMethod[0];
        public IReadOnlyList<IXamlXConstructor> Constructors { get; } = new IXamlXConstructor[0];
        public IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; } = new IXamlXCustomAttribute[0];
        public IXamlXType BaseType { get; }
        public bool IsAssignableFrom(IXamlXType type) => type == this;

        public IXamlXType MakeGenericType(IReadOnlyList<IXamlXType> typeArguments)
        {
            throw new NotSupportedException();
        }

        XamlXNullType()
        {
            
        }
        public static XamlXNullType Instance { get; } = new XamlXNullType();
    }
    
    public static class XamlXTypeSystemExtensions
    {
        public static string GetFqn(this IXamlXType type) => $"{type.Assembly?.Name}:{type.Namespace}.{type.Name}";

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
        
        public static IXamlXConstructor FindConstructor(this IXamlXType type, List<IXamlXType> args)
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