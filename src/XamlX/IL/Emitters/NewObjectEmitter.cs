using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    class NewObjectEmitter : IXamlAstNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlILNodeEmitResult? Emit(IXamlAstNode node, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlAstNewClrObjectNode n))
                return null;

            var type = n.Type.GetClrType();
            var ctor = n.Constructor;
            
            for (var c = 0; c < n.Arguments.Count; c++)
            {
                context.Emit(n.Arguments[c], codeGen, ctor.Parameters[c]);
            }

            var gen = codeGen
                .Emit(OpCodes.Newobj, ctor);


            return XamlILNodeEmitResult.Type(0, type);
        }
    }
}
