using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class NewObjectEmitter : IXamlAstNodeEmitter
    {
        public XamlNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlAstNewClrObjectNode n))
                return null;
            var type = n.Type.GetClrType();

            var argTypes = n.Arguments.Select(a => a.Type.GetClrType()).ToList();
            var ctor = type.FindConstructor(argTypes);
            if (ctor == null)
            {
                if (argTypes.Count != 0)
                {
                    ctor = type.Constructors.FirstOrDefault(x =>
                        !x.IsStatic && x.IsPublic && x.Parameters.Count == argTypes.Count);
                    
                }

                if (ctor == null)
                    throw new XamlLoadException(
                        $"Unable to find public constructor for type {type.GetFqn()}({string.Join(", ", argTypes.Select(at => at.GetFqn()))})",
                        n);
            }

            for (var c = 0; c < n.Arguments.Count; c++)
            {
                if (!context.Configuration.TryGetCorrectlyTypedValue(n.Arguments[c], ctor.Parameters[c], out var arg))
                    throw new XamlLoadException(
                        $"Unable to convert {n.Arguments[c].Type.GetClrType().GetFqn()} to {ctor.Parameters[c].GetFqn()} for constructor of {n.Type.GetClrType().GetFqn()}",
                        n.Arguments[c]);
                context.Emit(arg, codeGen, ctor.Parameters[c]);
            }

            var gen = codeGen
                .Emit(OpCodes.Newobj, ctor);
            
            
            return XamlNodeEmitResult.Type(type);
        }
    }
}