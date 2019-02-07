using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class ObjectInitializationNodeEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlObjectInitializationNode init))
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
            
            
            return XamlNodeEmitResult.Void;
        }
    }
}