using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlNewObjectTransformer : IXamlIlAstTransformer
    {
        void SubTransform(XamlIlAstTransformationContext context, XamlIlAstObjectNode ni)
        {
            var valueIndexes = new List<int>();
            for (var c = 0; c < ni.Children.Count; c++)
                if (ni.Children[c] is IXamlIlAstValueNode)
                    valueIndexes.Add(c);

            if (valueIndexes.Count == 0)
                return;
            var type = ni.Type.GetClrType();
            IXamlIlAstValueNode VNode(int ind) => (IXamlIlAstValueNode) ni.Children[ind];

            var contentProperty = context.Configuration.FindContentProperty(type);
            if (contentProperty == null)
            {
                foreach (var ind in valueIndexes)
                    if (context.Configuration.TryCallAdd(type, VNode(ind), out var addCall))
                        ni.Children[ind] = addCall;
                    else
                        throw new XamlIlLoadException(
                            $"Type `{type.GetFqn()} does not have content property and suitable Add({VNode(ind).Type.GetClrType().GetFqn()}) not found",
                            VNode(ind));
            }
            else
                XamlIlTransformHelpers.GeneratePropertyAssignments(context, contentProperty, valueIndexes.Count,
                    num => VNode(valueIndexes[num]),
                    (i, v) => ni.Children[valueIndexes[i]] = v);
        }
        
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode ni)
            {
                SubTransform(context, ni);
                
                return new XamlIlValueWithManipulationNode(ni, 
                    new XamlIlAstNewClrObjectNode(ni, ni.Type, ni.Arguments),
                    new XamlIlManipulationGroupNode(ni)
                    {
                        Children = ni.Children.Cast<IXamlIlAstManipulationNode>().ToList()
                    });
            }

            return node;
        }
    }
}