using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class PropertyAssignmentEmitter : IXamlAstNodeEmitter
    {
        public bool Emit(IXamlAstNode node, XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlPropertyAssignmentNode an))
                return false;
            var callOp = an.Property.Setter.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
            context.Emit(an.Value, codeGen); 
            codeGen.Generator.Emit(callOp, an.Property.Setter);
            
            return true;
        }
    }
}