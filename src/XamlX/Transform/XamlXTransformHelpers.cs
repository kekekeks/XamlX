using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public static class XamlXTransformHelpers
    {
        public static void GeneratePropertyAssignments(XamlXAstTransformationContext context,
            IXamlXProperty contentProperty,
            int count, Func<int, IXamlXAstValueNode> getNode, Action<int, IXamlXAstNode> setNode)
        {
            var type = contentProperty.PropertyType;
            // Direct property assignment?
            if (contentProperty.Setter?.IsPublic == true
                && count == 1
                && context.Configuration.TryGetCorrectlyTypedValue(getNode(0),
                    contentProperty.PropertyType,
                    out var value))
                setNode(0,
                    new XamlXPropertyAssignmentNode(getNode(0), contentProperty, value));
            // Collection property?
            else if (contentProperty.Getter?.IsPublic == true)
            {
                for (var ind = 0; ind < count; ind++)
                {
                    if (context.Configuration.TryCallAdd(type, getNode(ind), out var addCall))
                        setNode(ind,
                            new XamlXPropertyValueManipulationNode(getNode(ind), contentProperty, addCall));
                    else
                        throw new XamlXLoadException(
                            $"Unable to find a suitable Add({getNode(ind).Type.GetClrType().GetFqn()}) on type {contentProperty.PropertyType.GetFqn()}",
                            getNode(ind));
                }
            }
            else
                throw new XamlXLoadException(
                    $"Unable to handle {getNode(0).Type.GetClrType().GetFqn()} assignment to {contentProperty.Name} " +
                    $"as either direct assignment or collection initialization, check if value type matches property type or that property type has proper Add method",
                    getNode(0));
        }


        public static List<IXamlXAstManipulationNode> GeneratePropertyAssignments(XamlXAstTransformationContext context,
            IXamlXProperty property, List<IXamlXAstValueNode> nodes)
        {
            var tmp = nodes.Cast<IXamlXAstNode>().ToList();
            GeneratePropertyAssignments(context, property, tmp.Count,
                i => (IXamlXAstValueNode) tmp[i],
                (i, v) => tmp[i] = v);
            return tmp.Cast<IXamlXAstManipulationNode>().ToList();
        }
    }
}