using System.Linq;
using XamlX.Ast;

namespace XamlX.Transform.Transformers
{
    public class XamlDeferredContentTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlPropertyAssignmentNode pa))
                return node;
            var deferredAttrs = context.Configuration.TypeMappings.DeferredContentPropertyAttributes;
            if (deferredAttrs.Count == 0)
                return node;
            if (!pa.Property.CustomAttributes.Any(ca => deferredAttrs.Any(da => da.Equals(ca.Type))))
                return node;
            
            pa.Value = new XamlDeferredContentNode(pa.Value, context.Configuration);
            return node;
        }
    }
}