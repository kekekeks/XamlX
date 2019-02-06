using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlNewObjectTransformer : IXamlAstTransformer
    {
        void SubTransform(XamlAstTransformationContext context, XamlAstObjectNode ni)
        {
            var valueIndexes = new List<int>();
            for (var c = 0; c < ni.Children.Count; c++)
                if (ni.Children[c] is IXamlAstValueNode)
                    valueIndexes.Add(c);

            if (valueIndexes.Count == 0)
                return;
            var type = ni.Type.GetClrType();
            IXamlAstValueNode VNode(int ind) => (IXamlAstValueNode) ni.Children[ind];

            var contentProperty = context.Configuration.FindContentProperty(type);
            if (contentProperty == null)
            {
                foreach (var ind in valueIndexes)
                    if (XamlTransformHelpers.TryCallAdd(context,null, type, VNode(ind), out var addCall))
                        ni.Children[ind] = addCall;
                    else
                        throw new XamlLoadException(
                            $"Type `{type.GetFqn()} does not have content property and suitable Add({VNode(ind).Type.GetClrType().GetFqn()}) not found",
                            VNode(ind));
            }
            else
                XamlTransformHelpers.GeneratePropertyAssignments(context, contentProperty, valueIndexes.Count,
                    num => VNode(valueIndexes[num]),
                    (i, v) => ni.Children[valueIndexes[i]] = v);
        }
        
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni)
            {
                SubTransform(context, ni);
                
                return new XamlValueWithManipulationNode(ni, 
                    new XamlAstNewClrObjectNode(ni, ni.Type, ni.Arguments),
                    new XamlManipulationGroupNode(ni)
                    {
                        Children = ni.Children.Cast<IXamlAstManipulationNode>().ToList()
                    });
            }

            return node;
        }
    }
}