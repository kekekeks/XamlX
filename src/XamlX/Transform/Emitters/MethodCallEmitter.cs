using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class MethodCallEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            if (!(node is XamlXMethodCallBaseNode mc))
                return null;

            bool thisArgFromArgs = node is XamlXStaticOrTargetedReturnMethodCallNode;
            bool expectsVoid = node is XamlXNoReturnMethodCallNode;


            for (var c = 0; c < mc.Arguments.Count; c++)
            {
                var off = thisArgFromArgs ? 0 : 1;
                var expectedType = mc.Method.ParametersWithThis[c + off];
                context.Emit(mc.Arguments[c], codeGen, expectedType);
            }

            


            mc.Method.Emit(context, codeGen, expectsVoid);
            
            var isVoid = mc.Method.ReturnType.Equals(context.Configuration.WellKnownTypes.Void);
            if (!expectsVoid && isVoid)
                throw new XamlXLoadException(
                    $"XamlXStaticReturnMethodCallNode expects a value while {mc.Method.Name} returns void", node);

            var consumed = thisArgFromArgs ? 0 : 1;
            return isVoid || expectsVoid
                ? XamlXNodeEmitResult.Void(consumed)
                : XamlXNodeEmitResult.Type(consumed, mc.Method.ReturnType);
        }
    }
}