using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class MethodCallEmitter : IXamlXAstNodeEmitter
    {
        public bool Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlXInstanceMethodCallNode mc))
                return false;
            foreach (var a in mc.Arguments)
                context.Emit(a, codeGen);
            codeGen.Generator.Emit(mc.Method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, mc.Method);
            return true;
        }
    }
}