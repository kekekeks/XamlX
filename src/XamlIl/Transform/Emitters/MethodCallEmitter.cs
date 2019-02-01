using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class MethodCallEmitter : IXamlIlAstNodeEmitter
    {
        public bool Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (!(node is XamlIlInstanceMethodCallNode mc))
                return false;
            foreach (var a in mc.Arguments)
                context.Emit(a, codeGen);
            codeGen.Generator.Emit(mc.Method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, mc.Method);
            return true;
        }
    }
}