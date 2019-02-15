using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class ManipulationGroupEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            if (!(node is XamlXManipulationGroupNode group))
                return null;
            if (group.Children.Count == 0)
                codeGen.Emit(OpCodes.Pop);
            else
            {
                for (var c = 0; c < group.Children.Count; c++)
                {
                    if (c != group.Children.Count - 1)
                        codeGen.Emit(OpCodes.Dup);
                    context.Emit(group.Children[c], codeGen, null);
                }
            }

            return XamlXNodeEmitResult.Void(1);
        }
    }
}
