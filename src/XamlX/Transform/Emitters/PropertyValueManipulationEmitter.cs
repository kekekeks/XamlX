using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class PropertyValueManipulationEmitter : IXamlXAstNodeEmitter
    {
        public bool Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlXPropertyValueManipulationNode pvm))
                return false;
            codeGen.Generator.Emit(pvm.Property.Getter.IsStatic ? OpCodes.Call : OpCodes.Callvirt,
                pvm.Property.Getter);
            context.Emit(pvm.Manipulation, codeGen);
            
            return true;
        }
    }
}