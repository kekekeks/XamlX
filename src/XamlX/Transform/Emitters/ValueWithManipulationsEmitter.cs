using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class ValueWithManipulationsEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlXValueWithManipulationNode vwm))
                return null;
            var created = context.Emit(vwm.Value, codeGen, vwm.Type.GetClrType());

            if (vwm.Manipulation != null &&
                !(vwm.Manipulation is XamlXManipulationGroupNode grp && grp.Children.Count == 0))
            {
                codeGen.Generator.Emit(OpCodes.Dup);
                context.Emit(vwm.Manipulation, codeGen, null);
            }
            return XamlXNodeEmitResult.Type(created.ReturnType);
        }
    }
}