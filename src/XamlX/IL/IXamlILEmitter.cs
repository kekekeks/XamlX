using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.IL
{

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlILEmitter : IHasLocalsPool
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
        IXamlLabel DefineLabel();
        IXamlILEmitter MarkLabel(IXamlLabel label);
        IXamlILEmitter Emit(OpCode code, IXamlLabel label);
        IXamlILEmitter Emit(OpCode code, IXamlLocal local);
        void InsertSequencePoint(IFileSource file, int line, int position);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlCustomEmitMethod : IXamlMethod
    {
        void EmitCall(IXamlILEmitter emitter);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlTypeILBuilder : IXamlTypeBuilder
    {
        new IXamlMethodILBuilder DefineMethod(IXamlType returnType, IEnumerable<IXamlType> args, string name, bool isPublic, bool isStatic,
            bool isInterfaceImpl, IXamlMethod overrideMethod = null);
        new IXamlConstructorILBuilder DefineConstructor(bool isStatic, params IXamlType[] args);
        new IXamlTypeILBuilder DefineSubType(IXamlType baseType, string name, bool isPublic);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlMethodILBuilder : IXamlMethodBuilder
    {
        IXamlILEmitter Generator { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlConstructorILBuilder : IXamlConstructorBuilder
    {
        IXamlILEmitter Generator { get; }
    }
}
