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
    static class XamlIlEmitterExtensions
    {
        public static IXamlILEmitter EmitCall(this IXamlILEmitter emitter, IXamlMethod method, bool swallowResult = false)
        {
            if (method is IXamlCustomEmitMethod<IXamlILEmitter> custom)
                custom.EmitCall(emitter);
            else
                emitter.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);

            if (swallowResult && !(method.ReturnType.Namespace == "System" && method.ReturnType.Name == "Void"))
                emitter.Pop();
            return emitter;
        }

        public static IXamlILEmitter DebugHatch(this IXamlILEmitter emitter, string message)
        {
#if DEBUG
            var debug = emitter.TypeSystem.GetType("XamlX.XamlDebugHatch").FindMethod(m => m.Name == "Debug");
            emitter.Emit(OpCodes.Ldstr, message);
            emitter.Emit(OpCodes.Call, debug);
#endif
            return emitter;
        }

        public static IXamlILEmitter Ldarg(this IXamlILEmitter emitter, int arg)
    => emitter.Emit(OpCodes.Ldarg, arg);

        public static IXamlILEmitter Ldarg_0(this IXamlILEmitter emitter)
            => emitter.Emit(OpCodes.Ldarg_0);

        public static IXamlILEmitter Ldfld(this IXamlILEmitter emitter, IXamlField field)
            => emitter.Emit(OpCodes.Ldfld, field);

        public static IXamlILEmitter Ldsfld(this IXamlILEmitter emitter, IXamlField field)
            => emitter.Emit(OpCodes.Ldsfld, field);

        public static IXamlILEmitter LdThisFld(this IXamlILEmitter emitter, IXamlField field)
            => emitter.Ldarg_0().Emit(OpCodes.Ldfld, field);

        public static IXamlILEmitter Stfld(this IXamlILEmitter emitter, IXamlField field)
            => emitter.Emit(OpCodes.Stfld, field);

        public static IXamlILEmitter Stsfld(this IXamlILEmitter emitter, IXamlField field)
            => emitter.Emit(OpCodes.Stsfld, field);

        public static IXamlILEmitter Ldloc(this IXamlILEmitter emitter, IXamlLocal local)
            => emitter.Emit(OpCodes.Ldloc, local);

        public static IXamlILEmitter Ldloca(this IXamlILEmitter emitter, IXamlLocal local)
            => emitter.Emit(OpCodes.Ldloca, local);

        public static IXamlILEmitter Stloc(this IXamlILEmitter emitter, IXamlLocal local)
            => emitter.Emit(OpCodes.Stloc, local);

        public static IXamlILEmitter Ldnull(this IXamlILEmitter emitter) => emitter.Emit(OpCodes.Ldnull);

        public static IXamlILEmitter Ldstr(this IXamlILEmitter emitter, string arg)
            => arg == null ? emitter.Ldnull() : emitter.Emit(OpCodes.Ldstr, arg);

        public static IXamlILEmitter Throw(this IXamlILEmitter emitter)
            => emitter.Emit(OpCodes.Throw);

        public static IXamlILEmitter Ldc_I4(this IXamlILEmitter emitter, int arg)
            => arg == 0
                ? emitter.Emit(OpCodes.Ldc_I4_0)
                : arg == 1
                    ? emitter.Emit(OpCodes.Ldc_I4_1)
                    : emitter.Emit(OpCodes.Ldc_I4, arg);

        public static IXamlILEmitter Ldc_R8(this IXamlILEmitter emitter, double arg)
            => emitter.Emit(OpCodes.Ldc_R8, arg);

        public static IXamlILEmitter Beq(this IXamlILEmitter emitter, IXamlLabel label)
            => emitter.Emit(OpCodes.Beq, label);

        public static IXamlILEmitter Blt(this IXamlILEmitter emitter, IXamlLabel label)
            => emitter.Emit(OpCodes.Blt, label);

        public static IXamlILEmitter Ble(this IXamlILEmitter emitter, IXamlLabel label)
            => emitter.Emit(OpCodes.Ble, label);

        public static IXamlILEmitter Bgt(this IXamlILEmitter emitter, IXamlLabel label)
            => emitter.Emit(OpCodes.Bgt, label);

        public static IXamlILEmitter Bge(this IXamlILEmitter emitter, IXamlLabel label)
            => emitter.Emit(OpCodes.Bge, label);

        public static IXamlILEmitter Br(this IXamlILEmitter emitter, IXamlLabel label)
            => emitter.Emit(OpCodes.Br, label);

        public static IXamlILEmitter Brfalse(this IXamlILEmitter emitter, IXamlLabel label)
            => emitter.Emit(OpCodes.Brfalse, label);

        public static IXamlILEmitter Brtrue(this IXamlILEmitter emitter, IXamlLabel label)
            => emitter.Emit(OpCodes.Brtrue, label);

        public static IXamlILEmitter Ret(this IXamlILEmitter emitter)
            => emitter.Emit(OpCodes.Ret);

        public static IXamlILEmitter Dup(this IXamlILEmitter emitter)
            => emitter.Emit(OpCodes.Dup);

        public static IXamlILEmitter Pop(this IXamlILEmitter emitter)
            => emitter.Emit(OpCodes.Pop);

        public static IXamlILEmitter Ldtoken(this IXamlILEmitter emitter, IXamlType type)
            => emitter.Emit(OpCodes.Ldtoken, type);

        public static IXamlILEmitter Ldtoken(this IXamlILEmitter emitter, IXamlMethod method)
            => emitter.Emit(OpCodes.Ldtoken, method);

        public static IXamlILEmitter Ldtype(this IXamlILEmitter emitter, IXamlType type)
        {
            var conv = emitter.TypeSystem.GetType("System.Type")
                .FindMethod(m => m.IsStatic && m.IsPublic && m.Name == "GetTypeFromHandle");
            return emitter.Ldtoken(type).EmitCall(conv);
        }

        public static IXamlILEmitter LdMethodInfo(this IXamlILEmitter emitter, IXamlMethod method)
        {
            var conv = emitter.TypeSystem.GetType("System.Reflection.MethodInfo")
                .FindMethod(m => m.IsStatic && m.IsPublic && m.Name == "GetMethodFromHandle");
            return emitter.Ldtoken(method).EmitCall(conv);
        }

        public static IXamlILEmitter Ldftn(this IXamlILEmitter emitter, IXamlMethod method)
            => emitter.Emit(OpCodes.Ldftn, method);

        public static IXamlILEmitter Isinst(this IXamlILEmitter emitter, IXamlType type)
            => emitter.Emit(OpCodes.Isinst, type);

        public static IXamlILEmitter Castclass(this IXamlILEmitter emitter, IXamlType type)
            => emitter.Emit(OpCodes.Castclass, type);

        public static IXamlILEmitter Box(this IXamlILEmitter emitter, IXamlType type)
            => emitter.Emit(OpCodes.Box, type);

        public static IXamlILEmitter Unbox_Any(this IXamlILEmitter emitter, IXamlType type)
            => emitter.Emit(OpCodes.Unbox_Any, type);


        public static IXamlILEmitter Unbox(this IXamlILEmitter emitter, IXamlType type)
            => emitter.Emit(OpCodes.Unbox, type);

        public static IXamlILEmitter Newobj(this IXamlILEmitter emitter, IXamlConstructor ctor)
            => emitter.Emit(OpCodes.Newobj, ctor);

        public static IXamlILEmitter Newarr(this IXamlILEmitter emitter, IXamlType type)
            => emitter.Emit(OpCodes.Newarr, type);

        public static IXamlILEmitter Ldelem_ref(this IXamlILEmitter emitter) => emitter.Emit(OpCodes.Ldelem_Ref);
        public static IXamlILEmitter Stelem_ref(this IXamlILEmitter emitter) => emitter.Emit(OpCodes.Stelem_Ref);
        public static IXamlILEmitter Ldlen(this IXamlILEmitter emitter) => emitter.Emit(OpCodes.Ldlen);

        public static IXamlILEmitter Add(this IXamlILEmitter emitter) => emitter.Emit(OpCodes.Add);

    }
}
