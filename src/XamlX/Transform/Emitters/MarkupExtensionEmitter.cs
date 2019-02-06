using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class MarkupExtensionEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlMarkupExtensionNode me))
                return null;
            
            
            var ptype = me.Property.PropertyType;
            var rtype = me.ProvideValue.ReturnType;
            context.Emit(me.Value, codeGen, me.Value.Type.GetClrType());
            if (me.ProvideValue.Parameters.Count != 0)
            {
                //TODO: IProvideValueTarget
                codeGen.Generator
                    .Emit(OpCodes.Ldloc, context.ContextLocal);
            }

            var resultLocal = codeGen.Generator.DefineLocal(rtype);
            codeGen.Generator
                .Emit(OpCodes.Call, me.ProvideValue)
                .Emit(OpCodes.Stloc, resultLocal);

            IXamlILEmitter CallSetter() => codeGen.Generator.Emit(me.Property.Setter.IsStatic ? OpCodes.Call : OpCodes.Callvirt,
                me.Property.Setter);


            
            // Now we have the value returned by markup extension in resultLocal

            
            //Simplest case: exact type match
            if (ptype.Equals(rtype))
            {
                codeGen.Generator
                    .Emit(OpCodes.Ldloc, resultLocal);
                CallSetter();
                return XamlNodeEmitResult.Void;
            }
            var exit = codeGen.Generator.DefineLabel();
            
            
            if (ptype.IsValueType && rtype.IsValueType)
            {
                // If both are value types, try convert non-nullable to nullable
                if (ptype.IsNullableOf(rtype))
                {
                    codeGen.Generator
                        .Emit(OpCodes.Ldloc, resultLocal)
                        .Emit(OpCodes.Newobj,
                            ptype.Constructors.First(c =>
                                c.Parameters.Count == 1 && c.Parameters[0].Equals(rtype)));
                    CallSetter();
                    return XamlNodeEmitResult.Void;
                }
            }
            else if (rtype.IsValueType && !ptype.IsValueType)
            {
                // If target is object, simply box
                if (ptype.Equals(context.Configuration.WellKnownTypes.Object))
                {
                    codeGen.Generator
                        .Emit(OpCodes.Ldloc, resultLocal)
                        .Emit(OpCodes.Box, rtype);
                    CallSetter();
                    return XamlNodeEmitResult.Void;
                }
            }
            else if (ptype.IsValueType)
            {
                // Cast attempt only makes sense if it's an object
                if (rtype.Equals(context.Configuration.WellKnownTypes.Object))
                {
                    var notMatchedType = codeGen.Generator.DefineLabel();
                    codeGen.Generator
                        .Emit(OpCodes.Ldloc, resultLocal)
                        .Emit(OpCodes.Isinst, ptype)
                        .Emit(OpCodes.Brfalse, notMatchedType)
                        .Emit(OpCodes.Ldloc, resultLocal)
                        .Emit(OpCodes.Unbox_Any, ptype);
                    CallSetter()
                        .Emit(OpCodes.Br, exit)
                        .MarkLabel(notMatchedType);
                }
            }
            else
            {               
                // if(res==null) target.Property = null;
                var notNull = codeGen.Generator.DefineLabel();
                codeGen.Generator
                    .Emit(OpCodes.Ldloc, resultLocal)
                    .Emit(OpCodes.Brtrue, notNull)
                    .Emit(OpCodes.Ldloc, resultLocal);
                CallSetter()
                    .Emit(OpCodes.Br, exit)
                    .MarkLabel(notNull);
                
                // if (res is T matched)  target.Property = matched;
                var nonMatchedType = codeGen.Generator.DefineLabel();
                codeGen.Generator
                    .Emit(OpCodes.Ldloc, resultLocal)
                    .Emit(OpCodes.Isinst, ptype)
                    .Emit(OpCodes.Dup)
                    .Emit(OpCodes.Brfalse, nonMatchedType);
                CallSetter();
                codeGen.Generator.Emit(OpCodes.Br, exit)
                    .MarkLabel(nonMatchedType)
                    .Emit(OpCodes.Pop);
            }
            
            // Cast attempts have failed, call external method
            codeGen.Generator
                .Emit(OpCodes.Ldstr, me.Property.Name)
                .Emit(OpCodes.Ldloc, context.ContextLocal)
                .Emit(OpCodes.Ldloc, resultLocal);
            if(rtype.IsValueType)
                codeGen.Generator.Emit(OpCodes.Box, rtype);
            codeGen.Generator    
                .Emit(OpCodes.Call, context.Configuration.TypeMappings.ApplyNonMatchingMarkupExtension)
                .Emit(OpCodes.Br, exit);


            codeGen.Generator.MarkLabel(exit);
            return XamlNodeEmitResult.Void;
        }
    }
}