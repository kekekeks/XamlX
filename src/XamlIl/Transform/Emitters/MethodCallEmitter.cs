using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class MethodCallEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (!(node is XamlIlInstanceMethodCallBaseNode mc))
                return null;
            for (var c = 0; c < mc.Arguments.Count; c++)
                context.Emit(mc.Arguments[c], codeGen, mc.Method.Parameters[c]);
            codeGen.Generator.Emit(mc.Method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, mc.Method);

            var isVoid = mc.Method.ReturnType.Equals(context.Configuration.WellKnownTypes.Void);
            if (mc is XamlIlInstanceNoReturnMethodCallNode && !isVoid)
                codeGen.Generator.Emit(OpCodes.Pop);
            if (mc is XamlIlStaticReturnMethodCallNode && isVoid)
                throw new XamlIlLoadException(
                    $"XamlIlStaticReturnMethodCallNode expects a value while {mc.Method.Name} returns void", node);

            return mc is XamlIlInstanceNoReturnMethodCallNode || isVoid
                ? XamlIlNodeEmitResult.Void
                : XamlIlNodeEmitResult.Type(mc.Method.ReturnType);
        }
    }
}