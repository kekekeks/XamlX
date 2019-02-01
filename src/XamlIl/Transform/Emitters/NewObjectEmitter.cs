using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class NewObjectEmitter : IXamlIlAstNodeEmitter
    {
        public bool Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (!(node is XamlIlAstNewInstanceNode n))
                return false;
            var type = n.Type.GetClrType();

            var argTypes = n.Arguments.Select(a => a.Type.GetClrType()).ToList();
            var ctor = type.FindConstructor(argTypes);
            if (ctor == null)
                throw new XamlIlLoadException(
                    $"Unable to find public constructor for type {type.GetFqn()}({string.Join(", ", argTypes.Select(at => at.GetFqn()))})",
                    n);

            foreach (var ctorArg in n.Arguments)
                context.Emit(ctorArg, codeGen);
            
            var gen = codeGen.Generator
                .Emit(OpCodes.Newobj, ctor);
            
            
            foreach (var ch in n.Children)
            {
                if (ch is IXamlIlAstManipulationNode mnode)
                {
                    gen.Emit(OpCodes.Dup);
                    context.Emit(mnode, codeGen);
                }
                else
                    throw new XamlIlLoadException($"Unable to emit node {ch}", ch);
            }
            return true;
        }
    }
}