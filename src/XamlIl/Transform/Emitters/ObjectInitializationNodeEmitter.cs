using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class ObjectInitializationNodeEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (!(node is XamlIlObjectInitializationNode init))
                return null;
            var supportInitType = context.Configuration.TypeMappings.SupportInitialize;
            var supportsInitialize = context.Configuration.TypeMappings.SupportInitialize.IsAssignableFrom(init.Type);

            if (supportsInitialize)
            {

                codeGen.Generator
                    // We need two copies of the object for Begin/EndInit
                    .Emit(OpCodes.Dup)
                    .Emit(OpCodes.Dup)
                    .EmitCall(supportInitType.FindMethod(m => m.Name == "BeginInit"));
            }

            context.Emit(init.Manipulation, codeGen, null);

            if (supportsInitialize)
                codeGen.Generator
                    .EmitCall(supportInitType.FindMethod(m => m.Name == "EndInit"));
            
            
            return XamlIlNodeEmitResult.Void;
        }
    }
}