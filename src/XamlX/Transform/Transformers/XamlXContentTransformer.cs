using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlXContentTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlXAstNewInstanceNode ni)
            {
                var valueIndexes = new List<int>();
                for (var c = 0; c < ni.Children.Count; c++)
                    if (ni.Children[c] is IXamlAstValueNode)
                        valueIndexes.Add(c);

                if (valueIndexes.Count == 0)
                    return node;
                var type = ni.Type.GetClrType();
                IXamlAstValueNode VNode(int ind) => (IXamlAstValueNode) ni.Children[ind];

                var contentProperty = context.Configuration.FindContentProperty(type);
                if (contentProperty == null)
                {
                    foreach (var ind in valueIndexes)
                        if (context.Configuration.TryCallAdd(type, VNode(ind), out var addCall))
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

            return node;
        }
    }
}