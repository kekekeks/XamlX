using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    class MethodCallEmitter : IXamlAstNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlMethodCallBaseNode mc))
                return null;

            bool thisArgFromArgs = node is XamlStaticOrTargetedReturnMethodCallNode;
            bool expectsVoid = node is XamlNoReturnMethodCallNode;


            for (var c = 0; c < mc.Arguments.Count; c++)
            {
                var off = thisArgFromArgs ? 0 : 1;
                var expectedType = mc.Method.ParametersWithThis[c + off];
                context.Emit(mc.Arguments[c], codeGen, expectedType);
            }

            context.Emit(mc.Method, codeGen, expectsVoid);
            
            var isVoid = mc.Method.ReturnType.Equals(context.Configuration.WellKnownTypes.Void);
            if (!expectsVoid && isVoid)
                throw new XamlLoadException(
                    $"XamlStaticReturnMethodCallNode expects a value while {mc.Method.Name} returns void", node);

            var consumed = thisArgFromArgs ? 0 : 1;
            return isVoid || expectsVoid
                ? XamlILNodeEmitResult.Void(consumed)
                : XamlILNodeEmitResult.Type(consumed, mc.Method.ReturnType);
        }
    }
}
