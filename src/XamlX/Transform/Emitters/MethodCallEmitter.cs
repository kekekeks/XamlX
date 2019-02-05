using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class MethodCallEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlXInstanceMethodCallBaseNode mc))
                return null;
            for (var c = 0; c < mc.Arguments.Count; c++)
                context.Emit(mc.Arguments[c], codeGen, mc.Method.Parameters[c]);
            codeGen.Generator.Emit(mc.Method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, mc.Method);

            var isVoid = mc.Method.ReturnType.Equals(context.Configuration.WellKnownTypes.Void);
            if (mc is XamlXInstanceNoReturnMethodCallNode && !isVoid)
                codeGen.Generator.Emit(OpCodes.Pop);
            if (mc is XamlXInstanceReturnMethodCallNode && isVoid)
                throw new XamlLoadException(
                    $"XamlXInstanceReturnMethodCallNode expects a value while {mc.Method.Name} returns void", node);

            return isVoid ? XamlNodeEmitResult.Void : XamlNodeEmitResult.Type(mc.Method.ReturnType);
        }
    }
}