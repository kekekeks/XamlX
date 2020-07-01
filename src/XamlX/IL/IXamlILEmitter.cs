using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using XamlX.Emit;
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
}
