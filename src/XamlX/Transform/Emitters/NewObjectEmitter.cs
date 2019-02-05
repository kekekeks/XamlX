using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class NewObjectEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXCodeGen codeGen)
        {
            if (!(node is XamlXAstNewInstanceNode n))
                return null;
            var type = n.Type.GetClrType();

            var argTypes = n.Arguments.Select(a => a.Type.GetClrType()).ToList();
            var ctor = type.FindConstructor(argTypes);
            if (ctor == null)
                throw new XamlXLoadException(
                    $"Unable to find public constructor for type {type.GetFqn()}({string.Join(", ", argTypes.Select(at => at.GetFqn()))})",
                    n);

            for (var c = 0; c < n.Arguments.Count; c++)
            {
                var ctorArg = n.Arguments[c];
                context.Emit(ctorArg, codeGen, ctor.Parameters[c]);
            }

            var gen = codeGen.Generator
                .Emit(OpCodes.Newobj, ctor);
            
            
            foreach (var ch in n.Children)
            {
                if (ch is IXamlXAstManipulationNode mnode)
                {
                    gen.Emit(OpCodes.Dup);
                    context.Emit(mnode, codeGen, null);
                }
                else
                    throw new XamlXLoadException($"Unable to emit node {ch}", ch);
            }

            return XamlXNodeEmitResult.Type(type);
        }
    }
}