using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlXDeferredContentTransformer : IXamlXAstTransformer
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
            
            pa.Value = new XamlXDeferredContentNode(pa.Value, context.Configuration);
            return node;
        }
    }
}