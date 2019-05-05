using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class MarkupExtensionEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter ilgen)
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
                if (context.Configuration.TypeMappings.ProvideValueTargetPropertyEmitter
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



            return XamlNodeEmitResult.Type(0, me.ProvideValue.ReturnType);
        }
    }
}
