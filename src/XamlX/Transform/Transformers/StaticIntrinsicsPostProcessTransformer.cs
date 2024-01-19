using System;

using XamlX.Ast;

namespace XamlX.Transform.Transformers;

internal class StaticIntrinsicsPostProcessTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlStaticExtensionNode staticExtension)
        {
            var member = staticExtension.ResolveMember(throwOnUnknown: true);
            if (member is null)
            {
                throw new InvalidOperationException();
            }
        }

        return node;
    }
}