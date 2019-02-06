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
        IReadOnlyList<IXamlXField> Fields { get; }
        IReadOnlyList<IXamlXMethod> Methods { get; }
        IReadOnlyList<IXamlXConstructor> Constructors { get; }
        IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; }
        IReadOnlyList<IXamlXType> GenericArguments { get; }
        bool IsAssignableFrom(IXamlXType type);
        IXamlXType MakeGenericType(IReadOnlyList<IXamlXType> typeArguments);
        IXamlXType GenericTypeDefinition { get; }
        IXamlXType BaseType { get; }
        bool IsValueType { get; }
        bool IsEnum { get; }
        IXamlXType GetEnumUnderlyingType();
        
    }

    public interface IXamlXMethod : IEquatable<IXamlXMethod>, IXamlXMember
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IXamlXType ReturnType { get; }
        IReadOnlyList<IXamlXType> Parameters { get; }
    }

    public interface IXamlXMember
    {
        string Name { get; }
    }
    
    public interface IXamlXConstructor : IEquatable<IXamlXConstructor>
    {
        bool IsPublic { get; }
        bool IsStatic { get; }
        IReadOnlyList<IXamlXType> Parameters { get; }
    }
    
    public interface IXamlXProperty : IEquatable<IXamlXProperty>, IXamlXMember
    {
        IXamlXType PropertyType { get; }
        IXamlXMethod Setter { get; }
        IXamlXMethod Getter { get; }
        IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; }
    }

    public interface IXamlXField : IEquatable<IXamlXField>, IXamlXMember
    {
        IXamlXType FieldType { get; }
        bool IsPublic { get; }
        bool IsStatic { get; }
        bool IsLiteral { get; }
        object GetLiteralValue();
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
    }

    public interface IXamlXLocal
    {
        
    }
    
    public interface IXamlXLabel
    {
        
    }

    public interface IXamlXClosure : IXamlXCodeGen
    {
        
    }
    
    public interface IXamlXCodeGen
    {
        IXamlXEmitter Generator { get; }
        void EmitClosure(IEnumerable<IXamlXType> fields);
    }

    public interface IXamlXMethodBuilder : IXamlXMethod
    {
        IXamlXEmitter Generator { get; }
    }
    
    public interface IXamlXConstructorBuilder : IXamlXConstructor
    {
        IXamlXEmitter Generator { get; }
    }

    public interface IXamlXTypeBuilder
    {
        IXamlXField DefineField(IXamlXType type, string name, bool isPublic, bool isStatic);
        void AddInterfaceImplementation(IXamlXType type);

        IXamlXMethodBuilder DefineMethod(IXamlXType returnType, IEnumerable<IXamlXType> args, string name, bool isPublic, bool isStatic,
            bool isInterfaceImpl);

        IXamlXProperty DefineProperty(IXamlXType propertyType, string name, IXamlXMethod setter, IXamlXMethod getter);
        IXamlXConstructorBuilder DefineConstructor(params IXamlXType[] args);
        IXamlXType CreateType();
    }
    
    
    public class XamlXPseudoType : IXamlXType
    {
        public XamlXPseudoType(string name)
        {
            Name = name;
        }
        public bool Equals(IXamlXType other) => other == this;

        public object Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public string Namespace { get; } = "";
        public IXamlXAssembly Assembly { get; } = null;
        public IReadOnlyList<IXamlXProperty> Properties { get; } = new IXamlXProperty[0];
        public IReadOnlyList<IXamlXField> Fields { get; } = new List<IXamlXField>();
        public IReadOnlyList<IXamlXMethod> Methods { get; } = new IXamlXMethod[0];
        public IReadOnlyList<IXamlXConstructor> Constructors { get; } = new IXamlXConstructor[0];
        public IReadOnlyList<IXamlXCustomAttribute> CustomAttributes { get; } = new IXamlXCustomAttribute[0];
        public IReadOnlyList<IXamlXType> GenericArguments { get; } = new IXamlXType[0];
        public IXamlXType BaseType { get; }
        public bool IsValueType { get; } = false;
        public bool IsEnum { get; } = false;
        public IXamlXType GetEnumUnderlyingType() => throw new InvalidOperationException();

        public bool IsAssignableFrom(IXamlXType type) => type == this;

        public IXamlXType MakeGenericType(IReadOnlyList<IXamlXType> typeArguments)
        {
            throw new NotSupportedException();
        }

        public IXamlXType GenericTypeDefinition => null;
        public static XamlXPseudoType Null { get; } = new XamlXPseudoType("{x:Null}");
        public static XamlXPseudoType Unknown { get; } = new XamlXPseudoType("{Unknown type}");
    }
    
    public static class XamlXTypeSystemExtensions
    {
        public static string GetFqn(this IXamlXType type) => $"{type.Assembly?.Name}:{type.Namespace}.{type.Name}";

        public static IXamlXType GetType(this IXamlXTypeSystem sys, string type)
        {
            var f = sys.FindType(type);
            if (f == null)
                throw new XamlXTypeSystemException("Unable to resolve type " + type);
            return f;
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
    }
    
}