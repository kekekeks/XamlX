using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class MethodCallEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlMethodCallBaseNode mc))
                return null;

            bool thisArgFromStack = node is XamlStaticOrTargetedReturnMethodCallNode && !mc.Method.IsStatic;
            bool expectsVoid = node is XamlNoReturnMethodCallNode;


            if (thisArgFromStack)
                context.Emit(mc.Arguments[0], codeGen, mc.Method.DeclaringType);

            for (var c = thisArgFromStack ? 1 : 0; c < mc.Arguments.Count; c++)
                context.Emit(mc.Arguments[c], codeGen, mc.Method.Parameters[c - (thisArgFromStack ? 1 : 0)]);



            codeGen.Emit(mc.Method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, mc.Method);
            
            var isVoid = mc.Method.ReturnType.Equals(context.Configuration.WellKnownTypes.Void);
            if (expectsVoid && !isVoid)
                codeGen.Emit(OpCodes.Pop);
            
            
            if (!expectsVoid && isVoid)
                throw new XamlLoadException(
                    $"XamlXStaticReturnMethodCallNode expects a value while {mc.Method.Name} returns void", node);

            return isVoid || expectsVoid
                ? XamlNodeEmitResult.Void
                : XamlNodeEmitResult.Type(mc.Method.ReturnType);
        }
    }
}