using System;
using System.Reflection.Emit;

namespace XamlX.TypeSystem
{
    public static class TypeSystemExtensions
    {
        public static IXamlILEmitter Ldarg(this IXamlILEmitter emitter, int arg)
            => emitter.Emit(OpCodes.Ldarg, arg);

        public static IXamlILEmitter Ldarg_0(this IXamlILEmitter emitter)
            => emitter.Emit(OpCodes.Ldarg_0);
        
        public static IXamlILEmitter Ldfld(this IXamlILEmitter emitter, IXamlField field)
            => emitter.Emit(OpCodes.Ldfld, field);
        
        public static IXamlILEmitter LdThisFld(this IXamlILEmitter emitter, IXamlField field)
            => emitter.Ldarg_0().Emit(OpCodes.Ldfld, field);
        
        public static IXamlILEmitter Stfld(this IXamlILEmitter emitter, IXamlField field)
            => emitter.Emit(OpCodes.Stfld, field);

        public static IXamlILEmitter Ldloc(this IXamlILEmitter emitter, IXamlLocal local)
            => emitter.Emit(OpCodes.Ldloc, local);
        
        public static IXamlILEmitter Stloc(this IXamlILEmitter emitter, IXamlLocal local)
            => emitter.Emit(OpCodes.Stloc, local);

        public static IXamlILEmitter Ldnull(this IXamlILEmitter emitter) => emitter.Emit(OpCodes.Ldnull);
        
        public static IXamlILEmitter Ldc_I4(this IXamlILEmitter emitter, int arg)
            => arg == 0
                ? emitter.Emit(OpCodes.Ldc_I4_0)
                : arg == 1
                    ? emitter.Emit(OpCodes.Ldc_I4_1)
                    : emitter.Emit(OpCodes.Ldc_I4, arg);

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
        
        public static IXamlILEmitter Ldtoken(this IXamlILEmitter emitter, IXamlType type)
            => emitter.Emit(OpCodes.Ldtoken, type);

        public static IXamlILEmitter Ldtype(this IXamlILEmitter emitter, IXamlType type)
        {
            var conv = emitter.TypeSystem.GetType("System.Type")
                .FindMethod(m => m.IsStatic && m.IsPublic && m.Name == "GetTypeFromHandle");
            return emitter.Ldtoken(type).EmitCall(conv);
        }





    }
}