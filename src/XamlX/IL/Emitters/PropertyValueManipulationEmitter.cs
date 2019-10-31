using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    class PropertyValueManipulationEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlPropertyValueManipulationNode pvm))
                return null;
            codeGen.EmitCall(pvm.Property.Getter);
            context.Emit(pvm.Manipulation, codeGen, null);
            
            return XamlNodeEmitResult.Void(1);
        }
    }
}
