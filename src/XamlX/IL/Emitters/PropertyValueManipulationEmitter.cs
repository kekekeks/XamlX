using System;
using XamlX.Ast;
using XamlX.Emit;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    class PropertyValueManipulationEmitter : IXamlAstNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlILNodeEmitResult? Emit(IXamlAstNode node, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlPropertyValueManipulationNode pvm))
                return null;

            var getter = pvm.Property.Getter
                ?? throw new InvalidOperationException($"Property {pvm.Property} doesn't have a getter");

            codeGen.EmitCall(getter);
            context.Emit(pvm.Manipulation, codeGen, null);
            
            return XamlILNodeEmitResult.Void(1);
        }
    }
}
