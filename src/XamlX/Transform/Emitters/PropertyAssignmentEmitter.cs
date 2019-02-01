using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class PropertyAssignmentEmitter : IXamlXAstNodeEmitter
    {
        public bool Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlXPropertyAssignmentNode an))
                return false;
            var callOp = an.Property.Setter.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
            context.Emit(an.Value, codeGen); 
            codeGen.Generator.Emit(callOp, an.Property.Setter);
            
            return true;
        }
    }
}