using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace XamlIl.TypeSystem
{
    public interface IXamlIlType : IEquatable<IXamlIlType>
    {
        string Name { get; }
        string Namespace { get; }
        IXamlIlAssembly Assembly { get; } 
    }

    public interface IXamlIlMethod : IEquatable<IXamlIlMethod>
    {
        string Name { get; }
        bool IsPublic { get; }
        IXamlIlType ReturnType { get; }
        IReadOnlyList<IXamlIlType> Parameters { get; set; }
    }

    public interface IXamlIlConstructor : IEquatable<IXamlIlConstructor>
    {
        bool IsPublic { get; }
        IReadOnlyList<IXamlIlType> Parameters { get; set; }
    }
    
    public interface IXamlIlProperty : IEquatable<IXamlIlProperty>
    {
        string Name { get; }
        IXamlIlType PropertyType { get; }
    }

    public interface IXamlIlAssembly : IEquatable<IXamlIlAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlIlType> Types { get; }
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
    }
    
    interface IXamlIlEmitter
    {
        IXamlIlEmitter Emit(OpCode code);
        IXamlIlEmitter Emit(OpCode code, IXamlIlMethod method);
    }

    interface IXamlIlClosure : IXamlIlCodeGen
    {
        
    }
    
    interface IXamlIlCodeGen
    {
        IXamlIlEmitter Generator { get; }
        void EmitClosure(IEnumerable<IXamlIlType> fields);
    }
    
}