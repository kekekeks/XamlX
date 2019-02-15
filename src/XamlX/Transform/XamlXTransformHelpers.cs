using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform.Transformers;
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
                && TryGetCorrectlyTypedValue(context, getNode(0),
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
            var so = context.Configuration.WellKnownTypes.Object;
            rv = null;
            IXamlXMethod FindAdder(IXamlXType valueType, IXamlXType keyType = null)
            {
                var candidates = targetPropertyType.FindMethods(m =>
                        !m.IsStatic && m.IsPublic
                                    && (m.Name == "Add" || m.Name.EndsWith(".Add"))).ToList();

                bool CheckArg(IXamlXType argType, bool allowObj)
                {
                    if (allowObj && argType.Equals(so))
                        return true;
                    if (!allowObj && !argType.Equals(so) && argType.IsAssignableFrom(valueType))
                        return true;
                    return false;
                }

                foreach (var allowObj in new[] {false, true})
                {
                    foreach (var m in candidates)
                    {
                        if (keyType == null && m.Parameters.Count == 1
                                            && CheckArg(m.Parameters[0], allowObj))
                            return m;
                        if (keyType != null && m.Parameters.Count == 2
                                                 && m.Parameters[0].IsAssignableFrom(keyType)
                                                 && CheckArg(m.Parameters[1], allowObj))
                            return m;

                    }
                }

                return null;
            }
            if (TryConvertMarkupExtension(context, value, targetProperty, out var ext))
            {
                var adder = FindAdder(ext.ProvideValue.ReturnType);
                if (adder != null)
                {
                    ext.Manipulation = adder;
                    rv = ext;
                    return true;
                }
            }
            else
            {
                var vtype = value.Type.GetClrType();
                IXamlXAstValueNode keyNode = null;

                bool IsKeyDirective(object node) => node is XamlXAstXmlDirective d
                                                                        && d.Namespace == XamlNamespaces.Xaml2006 &&
                                                                        d.Name == "Key";

                void ProcessDirective(object d)
                {
                    var directive = (XamlXAstXmlDirective) d;
                    if (directive.Values.Count != 1)
                        throw new XamlXParseException("Invalid number of arguments for x:Key directive",
                            directive);
                    keyNode = directive.Values[0];
                }

               
                void ProcessDirectiveCandidateList(IList nodes)
                {
                    var d = nodes.OfType<object>().FirstOrDefault(IsKeyDirective);
                    if (d != null)
                    {
                        ProcessDirective(d);
                        nodes.Remove(d);
                    }
                }
                
                IXamlXAstManipulationNode VisitManipulationNode(IXamlXAstManipulationNode man)
                {
                    if (IsKeyDirective(man))
                    {
                        ProcessDirective(man);
                        return new XamlXManipulationGroupNode(man);
                    }
                    if(man is XamlXManipulationGroupNode grp)
                        ProcessDirectiveCandidateList(grp.Children);
                    if (man is XamlXObjectInitializationNode init)
                        init.Manipulation = VisitManipulationNode(init.Manipulation);
                    return man;
                }
                
                if (value is XamlXAstObjectNode astObject)
                    ProcessDirectiveCandidateList(astObject.Children);
                else if (value is XamlXValueWithManipulationNode vman)
                {
                    vman.Manipulation = VisitManipulationNode(vman.Manipulation);
                }
                    
                
                var adder = FindAdder(vtype, keyNode?.Type.GetClrType());
                if (adder != null)
                {
                    var args = new List<IXamlXAstValueNode>();
                    if (keyNode != null)
                        args.Add(keyNode);
                    args.Add(value);
                    
                    rv = new XamlXNoReturnMethodCallNode(value, adder, args);
                    if (targetProperty != null)
                        rv = new XamlXPropertyValueManipulationNode(value, targetProperty, rv);
                    return true;
                }
            }
            
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

        public static bool TryGetCorrectlyTypedValue(XamlXAstTransformationContext context,
            IXamlXAstValueNode node, IXamlXType type, out IXamlXAstValueNode rv)
        {
            var cfg = context.Configuration;
            rv = null;
            if (type.IsAssignableFrom(node.Type.GetClrType()))
            {
                rv = node;
                return true;
            }

            if (cfg.CustomValueConverter?.Invoke(node, type, out rv) == true)
                return true;

            var nodeType = node.Type.GetClrType();
            // Implicit type converters
            if (!nodeType.Equals(cfg.WellKnownTypes.String))
                return false;


            if (node is XamlXAstTextNode tn)
            {
                if (type.IsEnum)
                {
                    var enumValue = type.Fields.FirstOrDefault(f => f.Name == tn.Text);
                    if (enumValue != null)
                    {
                        rv = TypeSystemHelpers.GetLiteralFieldConstantNode(enumValue, node);
                        return true;
                    }
                }

                // Well known types
                if (TypeSystemHelpers.ParseConstantIfTypeAllows(tn.Text, type, tn, out var constantNode))
                {
                    rv = constantNode;
                    return true;
                }

                if (type.FullName == "System.Type")
                {
                    var resolvedType = XamlXTypeReferenceResolver.ResolveType(context, tn.Text, tn, true);
                    rv = new XamlXTypeExtensionNode(tn, new XamlXAstClrTypeReference(tn, resolvedType), type);
                    return true;
                }
            }

            IXamlXAstValueNode CreateInvariantCulture() =>
                new XamlXStaticOrTargetedReturnMethodCallNode(node,
                    cfg.WellKnownTypes.CultureInfo.Methods.First(x =>
                        x.IsPublic && x.IsStatic && x.Name == "get_InvariantCulture"), null);

            var candidates = type.Methods.Where(m => m.Name == "Parse"
                                                     && m.ReturnType.Equals(type)
                                                     && m.Parameters.Count > 0
                                                     && m.Parameters[0].Equals(cfg.WellKnownTypes.String)).ToList();

            // Types with parse method
            var parser = candidates.FirstOrDefault(m =>
                             m.Parameters.Count == 2 &&
                             (
                                 m.Parameters[1].Equals(cfg.WellKnownTypes.CultureInfo)
                                 || m.Parameters[1].Equals(cfg.WellKnownTypes.IFormatProvider)
                             )
                         )
                         ?? candidates.FirstOrDefault(m => m.Parameters.Count == 1);
            if (parser != null)
            {
                var args = new List<IXamlXAstValueNode> {node};
                if (parser.Parameters.Count == 2)
                    args.Add(CreateInvariantCulture());

                rv = new XamlXStaticOrTargetedReturnMethodCallNode(node, parser, args);
                return true;
            }

            if (cfg.TypeMappings.TypeDescriptorContext != null)
            {
                var typeConverterAttribute =
                    cfg.GetCustomAttribute(type, cfg.TypeMappings.TypeConverterAttributes).FirstOrDefault();
                if (typeConverterAttribute != null)
                {
                    var arg = typeConverterAttribute.Parameters.FirstOrDefault();
                    var converterType = (arg as IXamlXType) ??
                                        (arg is String sarg ? cfg.TypeSystem.FindType(sarg) : null);
                    if (converterType != null)
                    {
                        var converterMethod = converterType.FindMethod("ConvertFrom", cfg.WellKnownTypes.Object, false,
                            cfg.TypeMappings.TypeDescriptorContext, cfg.WellKnownTypes.CultureInfo,
                            cfg.WellKnownTypes.Object);
                        rv =
                            new XamlXAstRuntimeCastNode(node,
                                new XamlXStaticOrTargetedReturnMethodCallNode(node, converterMethod,
                                    new[]
                                    {
                                        new XamlXAstNewClrObjectNode(node,
                                            converterType, null,
                                            new List<IXamlXAstValueNode>()),
                                        new XamlXAstContextLocalNode(node, cfg.TypeMappings.TypeDescriptorContext),
                                        CreateInvariantCulture(),
                                        node
                                    }), new XamlXAstClrTypeReference(node, type));
                        return true;
                    }
                }
            }

            return false;
        }
    }
}