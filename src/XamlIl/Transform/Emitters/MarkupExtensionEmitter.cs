using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.Transform.Transformers;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
#if !XAMLIL_INTERNAL
    public
#endif
    class MarkupExtensionEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlEmitter ilgen)
        {

            if (!(node is XamlIlMarkupExtensionNode me))
                return null;
            XamlIlNeedsParentStackCache.Verify(context, node);

            var prop = context.ParentNodes().OfType<XamlIlPropertyAssignmentNode>().FirstOrDefault();

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



            return XamlIlNodeEmitResult.Type(0, me.ProvideValue.ReturnType);
        }
    }
}
