using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class PropertyAssignmentEmitter : IXamlIlAstNodeEmitter
    {
        public bool Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (!(node is XamlIlPropertyAssignmentNode an))
                return false;
            var callOp = an.Property.Setter.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
            context.Emit(an.Value, codeGen); 
            codeGen.Generator.Emit(callOp, an.Property.Setter);
            
            return true;
        }
    }
}