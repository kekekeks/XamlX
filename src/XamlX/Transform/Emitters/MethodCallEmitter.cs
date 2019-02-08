using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class MethodCallEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlXMethodCallBaseNode mc))
                return null;

            bool thisArgFromStack = node is XamlXStaticOrTargetedReturnMethodCallNode && !mc.Method.IsStatic;
            bool expectsVoid = node is XamlXNoReturnMethodCallNode;


            if (thisArgFromStack)
                context.Emit(mc.Arguments[0], codeGen, mc.Method.DeclaringType);

            for (var c = thisArgFromStack ? 1 : 0; c < mc.Arguments.Count; c++)
                context.Emit(mc.Arguments[c], codeGen, mc.Method.Parameters[c - (thisArgFromStack ? 1 : 0)]);



            codeGen.Generator.Emit(mc.Method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, mc.Method);
            
            var isVoid = mc.Method.ReturnType.Equals(context.Configuration.WellKnownTypes.Void);
            if (expectsVoid && !isVoid)
                codeGen.Generator.Emit(OpCodes.Pop);
            
            
            if (!expectsVoid && isVoid)
                throw new XamlXLoadException(
                    $"XamlXStaticReturnMethodCallNode expects a value while {mc.Method.Name} returns void", node);

            return isVoid || expectsVoid
                ? XamlXNodeEmitResult.Void
                : XamlXNodeEmitResult.Type(mc.Method.ReturnType);
        }
    }
}