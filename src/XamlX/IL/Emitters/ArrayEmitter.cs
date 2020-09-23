using XamlX.Ast;
using XamlX.Emit;
using XamlX.TypeSystem;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    class ArrayEmitter : IXamlAstNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlArrayExtensionNode arrayNode))
            {
                return null;
            }

            var type = arrayNode.Type.GetClrType();
            var elementType = type.ArrayElementType;

            codeGen
                .Ldc_I4(arrayNode.Elements.Count)
                .Newarr(elementType);

            for (int i = 0; i < arrayNode.Elements.Count; i++)
            {
                var element = arrayNode.Elements[i];

                if (!elementType.IsAssignableFrom(element.Type.GetClrType()))
                {
                    throw new XamlLoadException("x:Array element is not assignable to the array element type!", element);
                }

                codeGen
                    .Dup()
                    .Ldc_I4(i);

                context.Emit(element, codeGen, elementType);

                if (elementType.IsValueType)
                {
                    codeGen.Stelem(elementType);
                }
                else
                {
                    codeGen.Stelem_ref();
                }
            }

            return XamlILNodeEmitResult.Type(0, type);
        }
    }
}
