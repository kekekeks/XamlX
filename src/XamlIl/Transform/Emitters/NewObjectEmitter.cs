using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
#if !XAMLIL_INTERNAL
    public
#endif
    class NewObjectEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!(node is XamlIlAstNewClrObjectNode n))
                return null;

            var type = n.Type.GetClrType();
            var ctor = n.Constructor ?? type.FindConstructor();
            if (ctor == null)
                throw new XamlIlLoadException("Unable to find default constructor and no non-default one is specified",
                    n);
            
            for (var c = 0; c < n.Arguments.Count; c++)
            {
                context.Emit(n.Arguments[c], codeGen, ctor.Parameters[c]);
            }

            var gen = codeGen
                .Emit(OpCodes.Newobj, ctor);


            return XamlIlNodeEmitResult.Type(0, type);
        }
    }
}
