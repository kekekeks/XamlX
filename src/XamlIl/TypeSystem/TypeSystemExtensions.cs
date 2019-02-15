using System;
using System.Reflection;
using System.Reflection.Emit;

namespace XamlIl.TypeSystem
{
    public static class TypeSystemExtensions
    {
        public static IXamlIlEmitter Ldarg(this IXamlIlEmitter emitter, int arg)
            => emitter.Emit(OpCodes.Ldarg, arg);

        public static IXamlIlEmitter Ldarg_0(this IXamlIlEmitter emitter)
            => emitter.Emit(OpCodes.Ldarg_0);
        
        public static IXamlIlEmitter Ldfld(this IXamlIlEmitter emitter, IXamlIlField field)
            => emitter.Emit(OpCodes.Ldfld, field);
        
        public static IXamlIlEmitter Ldsfld(this IXamlIlEmitter emitter, IXamlIlField field)
            => emitter.Emit(OpCodes.Ldsfld, field);
        
        public static IXamlIlEmitter LdThisFld(this IXamlIlEmitter emitter, IXamlIlField field)
            => emitter.Ldarg_0().Emit(OpCodes.Ldfld, field);
        
        public static IXamlIlEmitter Stfld(this IXamlIlEmitter emitter, IXamlIlField field)
            => emitter.Emit(OpCodes.Stfld, field);
            
        public static IXamlIlEmitter Stsfld(this IXamlIlEmitter emitter, IXamlIlField field)
            => emitter.Emit(OpCodes.Stsfld, field);

        public static IXamlIlEmitter Ldloc(this IXamlIlEmitter emitter, IXamlIlLocal local)
            => emitter.Emit(OpCodes.Ldloc, local);
        
        public static IXamlIlEmitter Ldloca(this IXamlIlEmitter emitter, IXamlIlLocal local)
            => emitter.Emit(OpCodes.Ldloca, local);
        
        public static IXamlIlEmitter Stloc(this IXamlIlEmitter emitter, IXamlIlLocal local)
            => emitter.Emit(OpCodes.Stloc, local);

        public static IXamlIlEmitter Ldnull(this IXamlIlEmitter emitter) => emitter.Emit(OpCodes.Ldnull);

        public static IXamlIlEmitter Ldstr(this IXamlIlEmitter emitter, string arg)
            => arg == null ? emitter.Ldnull() : emitter.Emit(OpCodes.Ldstr, arg);
        
        public static IXamlIlEmitter Ldc_I4(this IXamlIlEmitter emitter, int arg)
            => arg == 0
                ? emitter.Emit(OpCodes.Ldc_I4_0)
                : arg == 1
                    ? emitter.Emit(OpCodes.Ldc_I4_1)
                    : emitter.Emit(OpCodes.Ldc_I4, arg);

        public static IXamlIlEmitter Beq(this IXamlIlEmitter emitter, IXamlIlLabel label)
            => emitter.Emit(OpCodes.Beq, label);
        
        public static IXamlIlEmitter Blt(this IXamlIlEmitter emitter, IXamlIlLabel label)
            => emitter.Emit(OpCodes.Blt, label);
        
        public static IXamlIlEmitter Ble(this IXamlIlEmitter emitter, IXamlIlLabel label)
            => emitter.Emit(OpCodes.Ble, label);
        
        public static IXamlIlEmitter Bgt(this IXamlIlEmitter emitter, IXamlIlLabel label)
            => emitter.Emit(OpCodes.Bgt, label);
        
        public static IXamlIlEmitter Bge(this IXamlIlEmitter emitter, IXamlIlLabel label)
            => emitter.Emit(OpCodes.Bge, label);
        
        public static IXamlIlEmitter Br(this IXamlIlEmitter emitter, IXamlIlLabel label)
            => emitter.Emit(OpCodes.Br, label);
        
        public static IXamlIlEmitter Brfalse(this IXamlIlEmitter emitter, IXamlIlLabel label)
            => emitter.Emit(OpCodes.Brfalse, label);
        
        public static IXamlIlEmitter Brtrue(this IXamlIlEmitter emitter, IXamlIlLabel label)
            => emitter.Emit(OpCodes.Brtrue, label);
        
        public static IXamlIlEmitter Ret(this IXamlIlEmitter emitter)
            => emitter.Emit(OpCodes.Ret);
        
        public static IXamlIlEmitter Dup(this IXamlIlEmitter emitter)
            => emitter.Emit(OpCodes.Dup);
        
        public static IXamlIlEmitter Pop(this IXamlIlEmitter emitter)
            => emitter.Emit(OpCodes.Pop);
        
        public static IXamlIlEmitter Ldtoken(this IXamlIlEmitter emitter, IXamlIlType type)
            => emitter.Emit(OpCodes.Ldtoken, type);
        
        public static IXamlIlEmitter Ldtoken(this IXamlIlEmitter emitter, IXamlIlMethod method)
            => emitter.Emit(OpCodes.Ldtoken, method);

        public static IXamlIlEmitter Ldtype(this IXamlIlEmitter emitter, IXamlIlType type)
        {
            var conv = emitter.TypeSystem.GetType("System.Type")
                .FindMethod(m => m.IsStatic && m.IsPublic && m.Name == "GetTypeFromHandle");
            return emitter.Ldtoken(type).EmitCall(conv);
        }
        
        public static IXamlIlEmitter LdMethodInfo(this IXamlIlEmitter emitter, IXamlIlMethod method)
        {
            var conv = emitter.TypeSystem.GetType("System.Reflection.MethodInfo")
                .FindMethod(m => m.IsStatic && m.IsPublic && m.Name == "GetMethodFromHandle");
            return emitter.Ldtoken(method).EmitCall(conv);
        }

        public static IXamlIlEmitter Ldftn(this IXamlIlEmitter emitter, IXamlIlMethod method)
            => emitter.Emit(OpCodes.Ldftn, method);
        
        public static IXamlIlEmitter Isinst(this IXamlIlEmitter emitter, IXamlIlType type)
            => emitter.Emit(OpCodes.Isinst, type);
        
        public static IXamlIlEmitter Castclass(this IXamlIlEmitter emitter, IXamlIlType type)
            => emitter.Emit(OpCodes.Castclass, type);

        public static IXamlIlEmitter Box(this IXamlIlEmitter emitter, IXamlIlType type)
            => emitter.Emit(OpCodes.Box, type);
        
        public static IXamlIlEmitter Unbox_Any(this IXamlIlEmitter emitter, IXamlIlType type)
            => emitter.Emit(OpCodes.Unbox_Any, type);

        public static IXamlIlEmitter Newobj(this IXamlIlEmitter emitter, IXamlIlConstructor ctor)
            => emitter.Emit(OpCodes.Newobj, ctor);


    }
}
