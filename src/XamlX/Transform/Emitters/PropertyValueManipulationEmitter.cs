using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class PropertyValueManipulationEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlPropertyValueManipulationNode pvm))
                return null;
            codeGen.Generator.Emit(pvm.Property.Getter.IsStatic ? OpCodes.Call : OpCodes.Callvirt,
                pvm.Property.Getter);
            context.Emit(pvm.Manipulation, codeGen, null);
            
            return XamlNodeEmitResult.Void;
        }
    }
}