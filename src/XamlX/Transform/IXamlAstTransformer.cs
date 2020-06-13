using XamlX.Ast;
using XamlX.Transform;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstTransformer
    {
        IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node);
    }
}
