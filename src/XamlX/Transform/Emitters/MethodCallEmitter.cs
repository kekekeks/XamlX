using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class MethodCallEmitter : IXamlAstNodeEmitter
    {
        public bool Emit(IXamlAstNode node, XamlEmitContext context, IXamlXCodeGen codeGen)
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