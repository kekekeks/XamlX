using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXDeferredContentTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (!(node is XamlXPropertyAssignmentNode pa))
                return node;
            var deferredAttrs = context.Configuration.TypeMappings.DeferredContentPropertyAttributes;
            if (deferredAttrs.Count == 0)
                return node;
            if (!pa.Property.CustomAttributes.Any(ca => deferredAttrs.Any(da => da.Equals(ca.Type))))
                return node;

            pa.Values[pa.Values.Count - 1] =
                new XamlXDeferredContentNode(pa.Values[pa.Values.Count - 1], context.Configuration);
            return node;
        }
    }
}
