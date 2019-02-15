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



            bool thisArgFromArgs = node is XamlIlStaticOrTargetedReturnMethodCallNode;
            bool expectsVoid = node is XamlIlNoReturnMethodCallNode;


            for (var c = 0; c < mc.Arguments.Count; c++)
            {
                var off = thisArgFromArgs ? 0 : 1;
                var expectedType = mc.Method.ParametersWithThis[c + off];
                context.Emit(mc.Arguments[c], codeGen, expectedType);
            }

            


            mc.Method.Emit(context, codeGen, expectsVoid);
            
            var isVoid = mc.Method.ReturnType.Equals(context.Configuration.WellKnownTypes.Void);
            if (!expectsVoid && isVoid)
                throw new XamlIlLoadException(
                    $"XamlIlStaticReturnMethodCallNode expects a value while {mc.Method.Name} returns void", node);

            return isVoid || expectsVoid
                ? XamlIlNodeEmitResult.Void
                : XamlIlNodeEmitResult.Type(mc.Method.ReturnType);
        }
    }
}