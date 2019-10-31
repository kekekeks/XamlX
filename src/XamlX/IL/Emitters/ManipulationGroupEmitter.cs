using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    class ManipulationGroupEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlManipulationGroupNode group))
                return null;
            if (group.Children.Count == 0)
                codeGen.Pop();
            else
            {
                for (var c = 0; c < group.Children.Count; c++)
                {
                    if (c != group.Children.Count - 1)
                        codeGen.Emit(OpCodes.Dup);
                    context.Emit(group.Children[c], codeGen, null);
                }
            }

            return XamlNodeEmitResult.Void(1);
        }
    }
}
