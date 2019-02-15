using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class PropertyAssignmentEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!(node is XamlIlPropertyAssignmentNode an))
                return null;
            var callOp = an.Property.Setter.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
            context.Emit(an.Value, codeGen, an.Property.Setter.Parameters.Last()); 
            codeGen.Emit(callOp, an.Property.Setter);

            return XamlIlNodeEmitResult.Void(1);
        }
    }
}