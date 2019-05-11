using System.Linq;
using XamlIl.Ast;

namespace XamlIl.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlDeferredContentTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (!(node is XamlIlPropertyAssignmentNode pa))
                return node;
            var deferredAttrs = context.Configuration.TypeMappings.DeferredContentPropertyAttributes;
            if (deferredAttrs.Count == 0)
                return node;
            if (!pa.Property.CustomAttributes.Any(ca => deferredAttrs.Any(da => da.Equals(ca.Type))))
                return node;

            pa.Values[pa.Values.Count - 1] =
                new XamlIlDeferredContentNode(pa.Values[pa.Values.Count - 1], context.Configuration);
            return node;
        }
    }
}
