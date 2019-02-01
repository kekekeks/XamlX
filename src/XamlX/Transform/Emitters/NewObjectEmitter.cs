using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class NewObjectEmitter : IXamlAstNodeEmitter
    {
        public bool Emit(IXamlAstNode node, XamlEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlXAstNewInstanceNode n))
                return false;
            var type = n.Type.GetClrType();

            var argTypes = n.Arguments.Select(a => a.Type.GetClrType()).ToList();
            var ctor = type.FindConstructor(argTypes);
            if (ctor == null)
                throw new XamlLoadException(
                    $"Unable to find public constructor for type {type.GetFqn()}({string.Join(", ", argTypes.Select(at => at.GetFqn()))})",
                    n);

            foreach (var ctorArg in n.Arguments)
                context.Emit(ctorArg, codeGen);
            
            var gen = codeGen.Generator
                .Emit(OpCodes.Newobj, ctor);
            
            
            foreach (var ch in n.Children)
            {
                if (ch is IXamlAstManipulationNode mnode)
                {
                    gen.Emit(OpCodes.Dup);
                    context.Emit(mnode, codeGen);
                }
                else
                    throw new XamlLoadException($"Unable to emit node {ch}", ch);
            }
            return true;
        }
    }
}