using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace XamlX.TypeSystem
{
    public interface IXamlXType : IEquatable<IXamlXType>
    {
        string Name { get; }
        string Namespace { get; }
        IXamlXAssembly Assembly { get; } 
    }

    public interface IXamlXMethod : IEquatable<IXamlXMethod>
    {
        string Name { get; }
        bool IsPublic { get; }
        IXamlXType ReturnType { get; }
        IReadOnlyList<IXamlXType> Parameters { get; set; }
    }

    public interface IXamlXConstructor : IEquatable<IXamlXConstructor>
    {
        bool IsPublic { get; }
        IReadOnlyList<IXamlXType> Parameters { get; set; }
    }
    
    public interface IXamlXProperty : IEquatable<IXamlXProperty>
    {
        string Name { get; }
        IXamlXType PropertyType { get; }
    }

    public interface IXamlXAssembly : IEquatable<IXamlXAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlXType> Types { get; }
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
    }
    
    interface IXamlXEmitter
    {
        IXamlXEmitter Emit(OpCode code);
        IXamlXEmitter Emit(OpCode code, IXamlXMethod method);
    }

    interface IXamlXClosure : IXamlXCodeGen
    {
        
    }
    
    interface IXamlXCodeGen
    {
        IXamlXEmitter Generator { get; }
        void EmitClosure(IEnumerable<IXamlXType> fields);
    }
    
}