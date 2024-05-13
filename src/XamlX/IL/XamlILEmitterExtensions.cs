using System;
using System.Collections.Generic;
using System.Linq;
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
            else if (method is IXamlCustomEmitMethodWithContext<IXamlILEmitter, XamlILNodeEmitResult>)
                throw new InvalidOperationException("Use EmitCall overload extension with a context parameter");
            else
                emitter.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);

            if (swallowResult && !(method.ReturnType.Namespace == "System" && method.ReturnType.Name == "Void"))
                emitter.Pop();
            return emitter;
        }

        public static IXamlILEmitter EmitCall(this IXamlILEmitter emitter, IXamlMethod method, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, bool swallowResult = false)
        {
            if (method is IXamlCustomEmitMethod<IXamlILEmitter> custom)
                custom.EmitCall(emitter);
            else if (method is IXamlCustomEmitMethodWithContext<IXamlILEmitter, XamlILNodeEmitResult> customWithContext)
                customWithContext.EmitCall(context, emitter);
            else
                emitter.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);

            if (swallowResult && !(method.ReturnType.Namespace == "System" && method.ReturnType.Name == "Void"))
                emitter.Pop();
            return emitter;
        }

        public static IXamlILEmitter DebugHatch(this IXamlILEmitter emitter, string message)
        {
#if DEBUG
            var debug = emitter.TypeSystem.GetType("XamlX.XamlDebugHatch").GetMethod(m => m.Name == "Debug");
            emitter.Emit(OpCodes.Ldstr, message);
            emitter.Emit(OpCodes.Call, debug);
#endif
            return emitter;
        }

        public static IXamlILEmitter Ldarg(this IXamlILEmitter emitter, int arg)
            => arg switch
            {
                0 => emitter.Emit(OpCodes.Ldarg_0),
                1 => emitter.Emit(OpCodes.Ldarg_1),
                2 => emitter.Emit(OpCodes.Ldarg_2),
                3 => emitter.Emit(OpCodes.Ldarg_3),
                >= 4 and <= byte.MaxValue => emitter.Emit(OpCodes.Ldarg_S, (byte) arg),
                _ => emitter.Emit(OpCodes.Ldarg, arg)
            };

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
            => (local as IXamlILLocal)?.Index switch
            {
                0 => emitter.Emit(OpCodes.Ldloc_0),
                1 => emitter.Emit(OpCodes.Ldloc_1),
                2 => emitter.Emit(OpCodes.Ldloc_2),
                3 => emitter.Emit(OpCodes.Ldloc_3),
                >= 4 and <= byte.MaxValue => emitter.Emit(OpCodes.Ldloc_S, local),
                _ => emitter.Emit(OpCodes.Ldloc, local)
            };

        public static IXamlILEmitter Ldloca(this IXamlILEmitter emitter, IXamlLocal local)
        {
            var index = (local as IXamlILLocal)?.Index;
            return emitter.Emit(index is >= 0 and <= byte.MaxValue ? OpCodes.Ldloca_S : OpCodes.Ldloca, local);
        }

        public static IXamlILEmitter Stloc(this IXamlILEmitter emitter, IXamlLocal local) 
            => (local as IXamlILLocal)?.Index switch
            {
                0 => emitter.Emit(OpCodes.Stloc_0),
                1 => emitter.Emit(OpCodes.Stloc_1),
                2 => emitter.Emit(OpCodes.Stloc_2),
                3 => emitter.Emit(OpCodes.Stloc_3),
                >= 4 and <= byte.MaxValue => emitter.Emit(OpCodes.Stloc_S, local),
                _ => emitter.Emit(OpCodes.Stloc, local)
            };

        public static IXamlILEmitter Ldnull(this IXamlILEmitter emitter) => emitter.Emit(OpCodes.Ldnull);

        public static IXamlILEmitter Ldstr(this IXamlILEmitter emitter, string? arg)
            => arg == null ? emitter.Ldnull() : emitter.Emit(OpCodes.Ldstr, arg);

        public static IXamlILEmitter Throw(this IXamlILEmitter emitter)
            => emitter.Emit(OpCodes.Throw);

        public static IXamlILEmitter Ldc_I4(this IXamlILEmitter emitter, int arg)
            => arg switch
            {
                0 => emitter.Emit(OpCodes.Ldc_I4_0),
                1 => emitter.Emit(OpCodes.Ldc_I4_1),
                2 => emitter.Emit(OpCodes.Ldc_I4_2),
                3 => emitter.Emit(OpCodes.Ldc_I4_3),
                4 => emitter.Emit(OpCodes.Ldc_I4_4),
                5 => emitter.Emit(OpCodes.Ldc_I4_5),
                6 => emitter.Emit(OpCodes.Ldc_I4_6),
                7 => emitter.Emit(OpCodes.Ldc_I4_7),
                8 => emitter.Emit(OpCodes.Ldc_I4_8),
                -1 => emitter.Emit(OpCodes.Ldc_I4_M1),
                >= sbyte.MinValue and <= sbyte.MaxValue => emitter.Emit(OpCodes.Ldc_I4_S, (sbyte) arg),
                _ => emitter.Emit(OpCodes.Ldc_I4, arg)
            };

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
                .GetMethod(m => m.IsStatic && m.IsPublic && m.Name == "GetTypeFromHandle");
            return emitter.Ldtoken(type).EmitCall(conv);
        }

        public static IXamlILEmitter LdMethodInfo(this IXamlILEmitter emitter, IXamlMethod method)
        {
            var conv = emitter.TypeSystem.GetType("System.Reflection.MethodInfo")
                .GetMethod(m => m.IsStatic && m.IsPublic && m.Name == "GetMethodFromHandle");
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

        public static IXamlILEmitter EmitDefault(this IXamlILEmitter emitter, IXamlType type)
        {
            if (!type.IsValueType)
            {
                return emitter.Ldnull();
            }

            return type.FullName switch
            {
                "System.Boolean" or "System.Char" or "System.Int32" or "System.UInt32"
                    or "System.Byte" or "System.SByte" or "System.Int16" or "System.UInt16"
                    or "System.IntPtr" or "System.UIntPtr" => emitter.Emit(OpCodes.Ldc_I4_0),
                "System.Int64" or "System.UInt64" => emitter.Emit(OpCodes.Ldc_I8, 0L),
                "System.Single" => emitter.Emit(OpCodes.Ldc_R4, 0F),
                "System.Double" => emitter.Emit(OpCodes.Ldc_R8, 0D),
                "System.Decimal" => emitter.Ldsfld(type.Fields.First(f => f.Name == "Zero")),
                _ => EmitNewStruct(emitter, type)
            };

            static IXamlILEmitter EmitNewStruct(IXamlILEmitter emitter, IXamlType type)
            {
                var loc = emitter.DefineLocal(type);
                emitter.Ldloca(loc);
                emitter.Emit(OpCodes.Initobj, type);
                emitter.Ldloc(loc);
                return emitter;
            }
        }
    }
}
