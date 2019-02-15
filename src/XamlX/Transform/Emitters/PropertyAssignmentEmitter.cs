using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class PropertyAssignmentEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlPropertyAssignmentNode an))
                return null;
            var callOp = an.Property.Setter.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
            context.Emit(an.Value, codeGen, an.Property.Setter.Parameters.Last()); 
            codeGen.Emit(callOp, an.Property.Setter);

            return XamlNodeEmitResult.Void(1);
        }
    }
}