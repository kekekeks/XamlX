using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class PropertyAssignmentEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            if (!(node is XamlXPropertyAssignmentNode an))
                return null;
            var callOp = an.Property.Setter.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
            context.Emit(an.Value, codeGen, an.Property.Setter.Parameters.Last()); 
            codeGen.Emit(callOp, an.Property.Setter);

            return XamlXNodeEmitResult.Void(1);
        }
    }
}