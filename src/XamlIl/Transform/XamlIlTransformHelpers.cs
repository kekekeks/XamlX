using System;
using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public static class XamlIlTransformHelpers
    {
        public static void GeneratePropertyAssignments(XamlIlAstTransformationContext context,
            IXamlIlProperty contentProperty,
            int count, Func<int, IXamlIlAstValueNode> getNode, Action<int, IXamlIlAstNode> setNode)
        {
            var type = contentProperty.PropertyType;
            // Direct property assignment?
            if (contentProperty.Setter?.IsPublic == true
                && count == 1
                && context.Configuration.TryGetCorrectlyTypedValue(getNode(0),
                    contentProperty.PropertyType,
                    out var value))
                setNode(0,
                    new XamlIlPropertyAssignmentNode(getNode(0), contentProperty, value));
            // Collection property?
            else if (contentProperty.Getter?.IsPublic == true)
            {
                for (var ind = 0; ind < count; ind++)
                {
                    if (context.Configuration.TryCallAdd(type, getNode(ind), out var addCall))
                        setNode(ind,
                            new XamlIlPropertyValueManipulationNode(getNode(ind), contentProperty, addCall));
                    else
                        throw new XamlIlLoadException(
                            $"Unable to find a suitable Add({getNode(ind).Type.GetClrType().GetFqn()}) on type {contentProperty.PropertyType.GetFqn()}",
                            getNode(ind));
                }
            }
            else
                throw new XamlIlLoadException(
                    $"Unable to handle {getNode(0).Type.GetClrType().GetFqn()} assignment to {contentProperty.Name} " +
                    $"as either direct assignment or collection initialization, check if value type matches property type or that property type has proper Add method",
                    getNode(0));
        }


        public static List<IXamlIlAstManipulationNode> GeneratePropertyAssignments(XamlIlAstTransformationContext context,
            IXamlIlProperty property, List<IXamlIlAstValueNode> nodes)
        {
            var tmp = nodes.Cast<IXamlIlAstNode>().ToList();
            GeneratePropertyAssignments(context, property, tmp.Count,
                i => (IXamlIlAstValueNode) tmp[i],
                (i, v) => tmp[i] = v);
            return tmp.Cast<IXamlIlAstManipulationNode>().ToList();
        }
    }
}