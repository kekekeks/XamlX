using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
#if !XAMLIL_INTERNAL
    public
#endif
    class PropertyValueManipulationEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!(node is XamlIlPropertyValueManipulationNode pvm))
                return null;
            codeGen.EmitCall(pvm.Property.Getter);
            context.Emit(pvm.Manipulation, codeGen, null);
            
            return XamlIlNodeEmitResult.Void(1);
        }
    }
}
