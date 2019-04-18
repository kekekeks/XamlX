using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class PropertyValueManipulationEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            if (!(node is XamlXPropertyValueManipulationNode pvm))
                return null;
            codeGen.EmitCall(pvm.Property.Getter);
            context.Emit(pvm.Manipulation, codeGen, null);
            
            return XamlXNodeEmitResult.Void(1);
        }
    }
}