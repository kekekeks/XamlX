using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class ManipulationGroupEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (!(node is XamlIlManipulationGroupNode group))
                return null;
            if (group.Children.Count == 0)
                codeGen.Generator.Emit(OpCodes.Pop);
            else
            {
                for (var c = 0; c < group.Children.Count; c++)
                {
                    if (c != group.Children.Count - 1)
                        codeGen.Generator.Emit(OpCodes.Dup);
                    context.Emit(group.Children[0], codeGen, null);
                }
            }

            return XamlIlNodeEmitResult.Void;
        }
    }
}