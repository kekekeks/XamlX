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
        IReadOnlyList<IXamlField> Fields { get; }
        IReadOnlyList<IXamlMethod> Methods { get; }
        IReadOnlyList<IXamlConstructor> Constructors { get; }
        IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; }
        bool IsAssignableFrom(IXamlType type);
        IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments);
        IXamlType BaseType { get; }
        bool IsValueType { get; }
        bool IsEnum { get; }
        IXamlType GetEnumUnderlyingType();
    }

    public interface IXamlMethod : IEquatable<IXamlMethod>, IXamlMember
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlType ReturnType { get; }
        IReadOnlyList<IXamlType> Parameters { get; }
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
        IXamlILEmitter Emit(OpCode code, IXamlField field);
        IXamlILEmitter Emit(OpCode code, IXamlMethod method);
        IXamlILEmitter Emit(OpCode code, IXamlConstructor ctor);
        IXamlILEmitter Emit(OpCode code, string arg);
        IXamlILEmitter Emit(OpCode code, int arg);
        IXamlILEmitter Emit(OpCode code, long arg);
        IXamlILEmitter Emit(OpCode code, IXamlType type);
        IXamlILEmitter Emit(OpCode ldcR8, float arg);
        IXamlILEmitter Emit(OpCode ldcR8, double arg);
    }

    public interface IXamlXClosure : IXamlXCodeGen
    {
        
    }
    
    public interface IXamlXCodeGen
    {
        IXamlILEmitter Generator { get; }
        void EmitClosure(IEnumerable<IXamlType> fields);
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
        public IXamlAssembly Assembly { get; } = null;
        public IReadOnlyList<IXamlProperty> Properties { get; } = new IXamlProperty[0];
        public IReadOnlyList<IXamlField> Fields { get; } = new List<IXamlField>();
        public IReadOnlyList<IXamlMethod> Methods { get; } = new IXamlMethod[0];
        public IReadOnlyList<IXamlConstructor> Constructors { get; } = new IXamlConstructor[0];
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; } = new IXamlCustomAttribute[0];
        public IXamlType BaseType { get; }
        public bool IsValueType { get; } = false;
        public bool IsEnum { get; } = false;
        public IXamlType GetEnumUnderlyingType() => throw new InvalidOperationException();

        public bool IsAssignableFrom(IXamlType type) => type == this;

        public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments)
        {
            throw new NotSupportedException();
        }
        public static XamlPseudoType Null { get; } = new XamlPseudoType("{x:Null}");
        public static XamlPseudoType Unknown { get; } = new XamlPseudoType("{Unknown type}");
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