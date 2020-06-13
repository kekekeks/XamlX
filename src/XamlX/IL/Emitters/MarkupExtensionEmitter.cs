using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.Transform;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    class MarkupExtensionEmitter : IXamlAstNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter ilgen)
        {

            if (!(node is XamlMarkupExtensionNode me))
                return null;
            XamlNeedsParentStackCache.Verify(context, node);

            var prop = context.ParentNodes().OfType<XamlPropertyAssignmentNode>().FirstOrDefault();

            var needProvideValueTarget = me.ProvideValue.Parameters.Count != 0
                                         && context.RuntimeContext.PropertyTargetObject != null
                                         && prop != null;

            void EmitPropertyDescriptor()
            {
                if (context.EmitMappings.ProvideValueTargetPropertyEmitter
                        ?.Invoke(context, ilgen, prop.Property) == true)
                    return;
                ilgen.Ldstr(prop.Property.Name);
            }

            context.Emit(me.Value, ilgen, me.Value.Type.GetClrType());
            
            if (me.ProvideValue.Parameters.Count > 0)
                ilgen
                    .Emit(OpCodes.Ldloc, context.ContextLocal);

            if (needProvideValueTarget)
            {
                ilgen
                    .Ldloc(context.ContextLocal);
                EmitPropertyDescriptor();
                ilgen
                    .Stfld(context.RuntimeContext.PropertyTargetProperty);
            }

            ilgen.EmitCall(me.ProvideValue);

            if (needProvideValueTarget)
            {
                ilgen
                    .Ldloc(context.ContextLocal)
                    .Ldnull()
                    .Stfld(context.RuntimeContext.PropertyTargetProperty);
            }

            return XamlILNodeEmitResult.Type(0, me.ProvideValue.ReturnType);
        }
    }
}
