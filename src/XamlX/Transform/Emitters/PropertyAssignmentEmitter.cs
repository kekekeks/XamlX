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
            context.Emit(an.Value, codeGen, an.Property.Setter.Parameters.Last()); 
            codeGen.EmitCall(an.Property.Setter);

            return XamlXNodeEmitResult.Void(1);
        }
    }
}
