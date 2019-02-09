using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class MethodCallEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!(node is XamlIlMethodCallBaseNode mc))
                return null;

            bool thisArgFromStack = node is XamlIlStaticOrTargetedReturnMethodCallNode && !mc.Method.IsStatic;
            bool expectsVoid = node is XamlIlNoReturnMethodCallNode;


            if (thisArgFromStack)
                context.Emit(mc.Arguments[0], codeGen, mc.Method.DeclaringType);

            for (var c = thisArgFromStack ? 1 : 0; c < mc.Arguments.Count; c++)
                context.Emit(mc.Arguments[c], codeGen, mc.Method.Parameters[c - (thisArgFromStack ? 1 : 0)]);



            codeGen.Emit(mc.Method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, mc.Method);
            
            var isVoid = mc.Method.ReturnType.Equals(context.Configuration.WellKnownTypes.Void);
            if (expectsVoid && !isVoid)
                codeGen.Emit(OpCodes.Pop);
            
            
            if (!expectsVoid && isVoid)
                throw new XamlIlLoadException(
                    $"XamlIlStaticReturnMethodCallNode expects a value while {mc.Method.Name} returns void", node);

            return isVoid || expectsVoid
                ? XamlIlNodeEmitResult.Void
                : XamlIlNodeEmitResult.Type(mc.Method.ReturnType);
        }
    }
}