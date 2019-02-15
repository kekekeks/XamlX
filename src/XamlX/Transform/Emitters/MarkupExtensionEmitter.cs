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
            var rtype = me.ProvideValue?.ReturnType ?? me.Value.Type.GetClrType();
            var needProvideValueTarget = me.ProvideValue != null
                                         && me.ProvideValue.Parameters.Count != 0
                                         && context.RuntimeContext.PropertyTargetObject != null
                                         && me.Property != null;

            void EmitPropertyDescriptor()
            {
                if (me.Property == null)
                    ilgen.Ldnull();
                else if (context.Configuration.TypeMappings.ProvideValueTargetPropertyEmitter
                             ?.Invoke(context, codeGen, me.Property) == true)
                    return;
                else if (me.Property is XamlXAstAttachedProperty)
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
                    if (me.ProvideValue?.Parameters.Count > 0)
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

                    if (me.ProvideValue != null)
                        ilgen
                            .Emit(OpCodes.Call, me.ProvideValue);
                    ilgen
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
                        ilgen.MarkLabel(exit);
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
    }
}