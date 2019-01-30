using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace XamlX.TypeSystem
{
    public interface IXamlType : IEquatable<IXamlType>
    {
        string Name { get; }
        string Namespace { get; }
        IXamlAssembly Assembly { get; } 
    }

    public interface IXamlMethod : IEquatable<IXamlMethod>
    {
        string Name { get; }
        bool IsPublic { get; }
        IXamlType ReturnType { get; }
        IReadOnlyList<IXamlType> Parameters { get; set; }
    }

    public interface IXamlConstructor : IEquatable<IXamlConstructor>
    {
        bool IsPublic { get; }
        IReadOnlyList<IXamlType> Parameters { get; set; }
    }
    
    public interface IXamlProperty : IEquatable<IXamlProperty>
    {
        string Name { get; }
        IXamlType PropertyType { get; }
    }

    public interface IXamlAssembly : IEquatable<IXamlAssembly>
    {
        string Name { get; }
        IReadOnlyList<IXamlType> Types { get; }
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
    }
    
    interface IXamlILEmitter
    {
        IXamlILEmitter Emit(OpCode code);
        IXamlILEmitter Emit(OpCode code, IXamlMethod method);
    }

    interface IXamlXClosure : IXamlXCodeGen
    {
        
    }
    
    interface IXamlXCodeGen
    {
        IXamlILEmitter Generator { get; }
        void EmitClosure(IEnumerable<IXamlType> fields);
    }
    
}