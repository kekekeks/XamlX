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
            // Markup extension ?
            if (contentProperty.Setter?.IsPublic == true
                     && count == 1
                     && TryConvertMarkupExtension(context, getNode(0),
                         contentProperty, out var me))
                setNode(0, me);
            // Direct property assignment?
            else if (contentProperty.Setter?.IsPublic == true
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
                    if (TryCallAdd(context, contentProperty, contentProperty.PropertyType, getNode(ind), out var addCall))
                        setNode(ind, addCall);
                    else
                    {
                        var propFqn = contentProperty.PropertyType.GetFqn();
                        var valueFqn = getNode(ind).Type.GetClrType().GetFqn();
                        throw new XamlXLoadException(
                            $"Unable to directly convert {valueFqn} to {propFqn} find a suitable Add({valueFqn}) on type {propFqn}",
                            getNode(ind));
                    }
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

        public static bool TryCallAdd(XamlXAstTransformationContext context,
            IXamlXProperty targetProperty, IXamlXType targetPropertyType, IXamlXAstValueNode value, out IXamlXAstManipulationNode rv)
        {
            if (TryConvertMarkupExtension(context, value, targetProperty, out var ext))
            {
                var adder = new[] {ext.ProvideValue.ReturnType, context.Configuration.WellKnownTypes.Object}
                    .Select(argType => targetPropertyType.FindMethod(m =>
                        !m.IsStatic && m.IsPublic
                        && (m.Name == "Add" || m.Name.EndsWith(".Add"))
                        && m.Parameters.Count == 1
                        && m.Parameters[0].Equals(argType)))
                    .FirstOrDefault(m => m != null);
                if (adder != null)
                {
                    ext.Manipulation = adder;
                    rv = ext;
                    return true;
                }
            }

            if (context.Configuration.TryCallAdd(targetPropertyType, value, out var nret))
            {
                if (targetProperty != null)
                    rv = new XamlXPropertyValueManipulationNode(value, targetProperty, nret);
                else
                    rv = nret;
                return true;
            }

            rv = null;
            return false;
        }

        public static bool TryConvertMarkupExtension(XamlXAstTransformationContext context,
            IXamlXAstValueNode node, IXamlXProperty prop, out XamlXMarkupExtensionNode o)
        {
            o = null;
            var nodeType = node.Type.GetClrType();
            var candidates = nodeType.Methods.Where(m => m.Name == "ProvideValue" && m.IsPublic && !m.IsStatic)
                .ToList();
            var so = context.Configuration.WellKnownTypes.Object;
            var sp = context.Configuration.TypeMappings.ServiceProvider;

            // Try non-object variant first and variants without IServiceProvider argument first
            
            var provideValue = candidates.FirstOrDefault(m => m.Parameters.Count == 0 && !m.ReturnType.Equals(so))
                               ?? candidates.FirstOrDefault(m => m.Parameters.Count == 0)
                               ?? candidates.FirstOrDefault(m =>
                                   m.Parameters.Count == 1 && m.Parameters[0].Equals(sp) && !m.ReturnType.Equals(so))
                               ?? candidates.FirstOrDefault(m => m.Parameters.Count == 1 && m.Parameters[0].Equals(sp));

            if (provideValue == null)
                return false;
            o = new XamlXMarkupExtensionNode(node, prop, provideValue, node, null);
            return true;
        }
    }
}