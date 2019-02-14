using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class MarkupExtensionEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            if (!(node is XamlXMarkupExtensionNode me))
                return null;
            var ilgen = codeGen;
            var so = context.Configuration.WellKnownTypes.Object;
            var ptype = me.Manipulation?.Parameters[0] ?? me.Property.PropertyType;
            var rtype = me.ProvideValue.ReturnType;
            var needProvideValueTarget = me.ProvideValue.Parameters.Count != 0 &&
                                         context.RuntimeContext.PropertyTargetObject != null
                                         && me.Property != null;

            void EmitPropertyDescriptor()
            {
                if(context.Configuration.TypeMappings.ProvideValueTargetPropertyEmitter(context, me.Property))
                    return;
                if (me.Property is XamlXAstAttachedProperty)
                    ilgen.Ldtoken(me.Property.Getter ?? me.Property.Setter)
                        .Emit(OpCodes.Box, context.Configuration.TypeSystem.GetType("System.RuntimeMethodHandle"));
                else
                    ilgen.Ldstr(me.Property?.Name);
            }

            using (var resultLocalContainer = context.GetLocal(rtype))
            {
                var resultLocal = resultLocalContainer.Local;
                using (var targetObjectLocal = needProvideValueTarget ? context.GetLocal(so) : null)
                {
                    if (needProvideValueTarget)
                        ilgen
                            .Dup().Stloc(targetObjectLocal.Local);

                    context.Emit(me.Value, codeGen, me.Value.Type.GetClrType());
                    if (me.ProvideValue.Parameters.Count != 0)
                        ilgen
                            .Emit(OpCodes.Ldloc, context.ContextLocal);

                    if (needProvideValueTarget)
                    {
                        ilgen
                            .Ldloc(context.ContextLocal)
                            .Ldloc(targetObjectLocal.Local)
                            .Stfld(context.RuntimeContext.PropertyTargetObject)
                            .Ldloc(context.ContextLocal);
                        EmitPropertyDescriptor();
                        ilgen
                            .Stfld(context.RuntimeContext.PropertyTargetProperty);
                    }


                    ilgen
                        .Emit(OpCodes.Call, me.ProvideValue)
                        .Emit(OpCodes.Stloc, resultLocal);

                    if (needProvideValueTarget)
                    {
                        ilgen
                            .Ldloc(context.ContextLocal)
                            .Ldnull()
                            .Stfld(context.RuntimeContext.PropertyTargetObject)
                            .Ldloc(context.ContextLocal)
                            .Ldnull()
                            .Stfld(context.RuntimeContext.PropertyTargetProperty);
                    }
                }

                // At this point we have the target object at the top of the stack and markup extension result in resultLocal
                
                var exit = ilgen.DefineLabel();
                
                // This is needed for custom conversions of Binding to object
                var customTypes = context.Configuration.TypeMappings.MarkupExtensionCustomResultTypes;
                // This is needed for properties that accept Binding
                if (
                    me.Property != null &&
                    context.Configuration.TypeMappings.ShouldIgnoreMarkupExtensionCustomResultForProperty !=
                    null)
                    customTypes = customTypes.Where(ct =>
                            !context.Configuration.TypeMappings
                                .ShouldIgnoreMarkupExtensionCustomResultForProperty(me.Property, ct))
                        .ToList();
                
                
                if (customTypes.Any() && !rtype.IsValueType)
                {
                    void EmitCustomActionCall()
                    {
                        EmitPropertyDescriptor();
                        codeGen
                            .Emit(OpCodes.Ldloc, context.ContextLocal)
                            .Emit(OpCodes.Ldloc, resultLocal);
                        if (rtype.IsValueType)
                            codeGen.Emit(OpCodes.Box, rtype);
                        codeGen
                            .Emit(OpCodes.Call, context.Configuration.TypeMappings.MarkupExtensionCustomResultHandler)
                            .Emit(OpCodes.Br, exit);
                    }
                    
                    
                    // Skip conversion attempts and call custom conversion directly
                    if (customTypes.Any(ct => ct.IsAssignableFrom(rtype)))
                    {
                        EmitCustomActionCall();
                        return XamlXNodeEmitResult.Void;
                    }
                    
                    var callCustomLabel = ilgen.DefineLabel();
                    var afterCustomLabel = ilgen.DefineLabel();
                    foreach (var ct in customTypes)
                    {
                        codeGen
                            .Ldloc(resultLocal)
                            .Isinst(ct)
                            .Brtrue(callCustomLabel);
                    }
                    ilgen
                        .Br(afterCustomLabel)
                        .MarkLabel(callCustomLabel);
                    EmitCustomActionCall();
                    ilgen.MarkLabel(afterCustomLabel);
                }
                

                TypeSystemHelpers.EmitConvert(node, rtype, ptype,
                    lda => ilgen.Emit(lda ? OpCodes.Ldloca : OpCodes.Ldloc, resultLocal));

                // Call some method either on the target or on target's property
                if (me.Manipulation != null)
                {
                    // {target}.{Property}.{Method)(res)
                    if (me.Property != null)
                        using (var res = context.GetLocal(ptype))
                            ilgen
                                .Emit(OpCodes.Stloc, res.Local)
                                .EmitCall(me.Property.Getter)
                                .Emit(OpCodes.Ldloc, res.Local);
                    ilgen
                        .EmitCall(me.Manipulation, true);
                }
                // Call property setter on the target
                else
                    ilgen.EmitCall(me.Property.Setter);

                ilgen.MarkLabel(exit);

            }

            return XamlXNodeEmitResult.Void;
        }

        /*
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            if (!(node is XamlXMarkupExtensionNode me))
                return null;

            var so = context.Configuration.WellKnownTypes.Object;
            var ptype = me.Manipulation?.Parameters[0] ?? me.Property.PropertyType;
            var rtype = me.ProvideValue.ReturnType;

            var needProvideValueTarget = me.ProvideValue.Parameters.Count != 0 &&
                                         context.RuntimeContext.PropertyTargetObject != null
                                         && me.Property != null;

            void EmitPropertyDescriptor()
            {
                if (me.Property is XamlXAstAttachedProperty)
                    codeGen.Ldtoken(me.Property.Getter ?? me.Property.Setter)
                        .Emit(OpCodes.Box, context.Configuration.TypeSystem.GetType("System.RuntimeMethodHandle"));
                else
                    codeGen.Ldstr(me.Property?.Name);
            }
            
            using (var resultLocalContainer = context.GetLocal(codeGen, rtype))   
            {
                var resultLocal = resultLocalContainer.Local;
                using (var targetObjectLocal = needProvideValueTarget ? context.GetLocal(codeGen, so) : null)
                {
                    if (needProvideValueTarget)
                        codeGen
                            .Dup().Stloc(targetObjectLocal.Local);

                    context.Emit(me.Value, codeGen, me.Value.Type.GetClrType());
                    if (me.ProvideValue.Parameters.Count != 0)
                        codeGen
                            .Emit(OpCodes.Ldloc, context.ContextLocal);

                    if (needProvideValueTarget)
                    {
                        codeGen
                            .Ldloc(context.ContextLocal)
                            .Ldloc(targetObjectLocal.Local)
                            .Stfld(context.RuntimeContext.PropertyTargetObject)
                            .Ldloc(context.ContextLocal);
                        EmitPropertyDescriptor();
                        codeGen
                            .Stfld(context.RuntimeContext.PropertyTargetProperty);
                    }

                    
                    codeGen
                        .Emit(OpCodes.Call, me.ProvideValue)
                        .Emit(OpCodes.Stloc, resultLocal);

                    if (needProvideValueTarget)
                    {
                        codeGen
                            .Ldloc(context.ContextLocal)
                            .Ldnull()
                            .Stfld(context.RuntimeContext.PropertyTargetObject)
                            .Ldloc(context.ContextLocal)
                            .Ldnull()
                            .Stfld(context.RuntimeContext.PropertyTargetProperty);
                    }
                }
                
                

                IXamlXEmitter CallSetter()
                {
                    if (me.Manipulation != null)
                    {
                        // {target}.{Property}.{Method)(res)
                        
                        if (me.Property != null)
                            using (var res = context.GetLocal(codeGen, ptype))
                                codeGen
                                    .Emit(OpCodes.Stloc, res.Local)
                                    .EmitCall(me.Property.Getter)
                                    .Emit(OpCodes.Ldloc, res.Local);
                        codeGen
                            .EmitCall(me.Manipulation, true);
                        return codeGen;
                    }

                    return codeGen.EmitCall(me.Property.Setter);
                }


                // Now we have the value returned by markup extension in resultLocal


                var exit = codeGen.DefineLabel();

                // This is needed for custom conversions of Binding to object
                var customTypes = context.Configuration.TypeMappings.CustomConvertedMarkupExtensionReturnTypes;
                // This is needed for properties that accept Binding
                if (
                    me.Property != null &&
                    context.Configuration.TypeMappings.IgnoreCustomMarkupExtensionReturnTypesConversionForProperty !=
                    null)
                    customTypes = customTypes.Where(ct =>
                            !context.Configuration.TypeMappings
                                .IgnoreCustomMarkupExtensionReturnTypesConversionForProperty(me.Property, ct))
                        .ToList();
                
                
                if (customTypes.Any() && !rtype.IsValueType)
                {
                    // Skip conversion attempts and call custom conversion directly
                    if(customTypes.Any(ct=>ct.IsAssignableFrom(rtype)))
                        goto emitCustomCall;
                    foreach (var ct in customTypes)
                    {

                        codeGen
                            .Ldloc(resultLocal)
                            .Isinst(ct)
                            .Brtrue(emitCustomCallLabel);
                    }
                    
                }
                
                //Simplest case: exact type match
                if (ptype.Equals(rtype))
                {
                    codeGen
                        .Emit(OpCodes.Ldloc, resultLocal);
                    CallSetter();
                    return XamlXNodeEmitResult.Void;
                }

                if (ptype.IsValueType && rtype.IsValueType)
                {
                    // If both are value types, try convert non-nullable to nullable
                    if (ptype.IsNullableOf(rtype))
                    {
                        codeGen
                            .Emit(OpCodes.Ldloc, resultLocal)
                            .Emit(OpCodes.Newobj,
                                ptype.Constructors.First(c =>
                                    c.Parameters.Count == 1 && c.Parameters[0].Equals(rtype)));
                        CallSetter();
                    }
                }
                else if (rtype.IsValueType && !ptype.IsValueType)
                {
                    // If target is object, simply box
                    if (ptype.Equals(context.Configuration.WellKnownTypes.Object))
                    {
                        codeGen
                            .Emit(OpCodes.Ldloc, resultLocal)
                            .Emit(OpCodes.Box, rtype);
                        CallSetter();
                    }
                }
                else if (ptype.IsValueType)
                {
                    // Cast attempt only makes sense if it's an object
                    if (rtype.Equals(context.Configuration.WellKnownTypes.Object))
                    {
                        var notMatchedType = codeGen.DefineLabel();
                        codeGen
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
                    var notNull = codeGen.DefineLabel();
                    codeGen
                        .Emit(OpCodes.Ldloc, resultLocal)
                        .Emit(OpCodes.Brtrue, notNull)
                        .Emit(OpCodes.Ldloc, resultLocal);
                    CallSetter()
                        .Emit(OpCodes.Br, exit)
                        .MarkLabel(notNull);

                    // if (res is T matched)  target.Property = matched;
                    var nonMatchedType = codeGen.DefineLabel();
                    codeGen
                        .Emit(OpCodes.Ldloc, resultLocal)
                        .Emit(OpCodes.Isinst, ptype)
                        .Emit(OpCodes.Dup)
                        .Emit(OpCodes.Brfalse, nonMatchedType);
                    CallSetter();
                    codeGen.Emit(OpCodes.Br, exit)
                        .MarkLabel(nonMatchedType)
                        .Emit(OpCodes.Pop);
                }

/*
                // Cast attempts have failed, call external method
                EmitPropertyDescriptor();
                codeGen
                    .Emit(OpCodes.Ldloc, context.ContextLocal)
                    .Emit(OpCodes.Ldloc, resultLocal);
                if (rtype.IsValueType)
                    codeGen.Emit(OpCodes.Box, rtype);
                codeGen
                    .Emit(OpCodes.Call, context.Configuration.TypeMappings.ApplyNonMatchingMarkupExtension)
                    .Emit(OpCodes.Br, exit);
                
                codeGen.MarkLabel(exit);
            }

            return XamlXNodeEmitResult.Void;
        }*/
    }
}