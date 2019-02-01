using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public static class XamlTransformHelpers
    {
        public static void GeneratePropertyAssignments(XamlAstTransformationContext context,
            IXamlProperty contentProperty,
            int count, Func<int, IXamlAstValueNode> getNode, Action<int, IXamlAstNode> setNode)
        {
            var type = contentProperty.PropertyType;
            // Direct property assignment?
            if (contentProperty.Setter?.IsPublic == true
                && count == 1
                && context.Configuration.TryGetCorrectlyTypedValue(getNode(0),
                    contentProperty.PropertyType,
                    out var value))
                setNode(0,
                    new XamlPropertyAssignmentNode(getNode(0), contentProperty, value));
            // Collection property?
            else if (contentProperty.Getter?.IsPublic == true)
            {
                for (var ind = 0; ind < count; ind++)
                {
                    if (context.Configuration.TryCallAdd(type, getNode(ind), out var addCall))
                        setNode(ind,
                            new XamlPropertyValueManipulationNode(getNode(ind), contentProperty, addCall));
                    else
                        throw new XamlLoadException(
                            $"Unable to find a suitable Add({getNode(ind).Type.GetClrType().GetFqn()}) on type {contentProperty.PropertyType.GetFqn()}",
                            getNode(ind));
                }
            }
            else
                throw new XamlLoadException(
                    $"Unable to handle {getNode(0).Type.GetClrType().GetFqn()} assignment to {contentProperty.Name} " +
                    $"as either direct assignment or collection initialization, check if value type matches property type or that property type has proper Add method",
                    getNode(0));
        }


        public static List<IXamlAstManipulationNode> GeneratePropertyAssignments(XamlAstTransformationContext context,
            IXamlProperty property, List<IXamlAstValueNode> nodes)
        {
            var tmp = nodes.Cast<IXamlAstNode>().ToList();
            GeneratePropertyAssignments(context, property, tmp.Count,
                i => (IXamlAstValueNode) tmp[i],
                (i, v) => tmp[i] = v);
            return tmp.Cast<IXamlAstManipulationNode>().ToList();
        }
    }
}