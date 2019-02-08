using System;
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
        
        public static IXamlIlEmitter LdThisFld(this IXamlIlEmitter emitter, IXamlIlField field)
            => emitter.Ldarg_0().Emit(OpCodes.Ldfld, field);
        
        public static IXamlIlEmitter Stfld(this IXamlIlEmitter emitter, IXamlIlField field)
            => emitter.Emit(OpCodes.Stfld, field);

        public static IXamlIlEmitter Ldloc(this IXamlIlEmitter emitter, IXamlIlLocal local)
            => emitter.Emit(OpCodes.Ldloc, local);
        
        public static IXamlIlEmitter Stloc(this IXamlIlEmitter emitter, IXamlIlLocal local)
            => emitter.Emit(OpCodes.Stloc, local);

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
        
        public static IXamlIlEmitter Ldtoken(this IXamlIlEmitter emitter, IXamlIlType type)
            => emitter.Emit(OpCodes.Ldtoken, type);

        public static IXamlIlEmitter Ldtype(this IXamlIlEmitter emitter, IXamlIlType type)
        {
            var conv = emitter.TypeSystem.GetType("System.Type")
                .FindMethod(m => m.IsStatic && m.IsPublic && m.Name == "GetTypeFromHandle");
            return emitter.Ldtoken(type).EmitCall(conv);
        }





    }
}