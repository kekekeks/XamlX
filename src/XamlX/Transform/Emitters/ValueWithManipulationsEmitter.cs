using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class ValueWithManipulationsEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlValueWithManipulationNode vwm))
                return null;
            var created = context.Emit(vwm.Value, codeGen, vwm.Type.GetClrType());

            if (vwm.Manipulation != null &&
                !(vwm.Manipulation is XamlManipulationGroupNode grp && grp.Children.Count == 0))
            {
                codeGen.Emit(OpCodes.Dup);
                context.Emit(vwm.Manipulation, codeGen, null);
            }
            return XamlNodeEmitResult.Type(0, created.ReturnType);
        }
    }
}