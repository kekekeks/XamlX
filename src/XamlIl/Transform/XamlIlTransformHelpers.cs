using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform.Transformers;
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
                    new XamlIlPropertyAssignmentNode(getNode(0), contentProperty, value));
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
                        throw new XamlIlLoadException(
                            $"Unable to directly convert {valueFqn} to {propFqn} find a suitable Add({valueFqn}) on type {propFqn}",
                            getNode(ind));
                    }
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

        public static bool TryCallAdd(XamlIlAstTransformationContext context,
            IXamlIlProperty targetProperty, IXamlIlType targetPropertyType, IXamlIlAstValueNode value, out IXamlIlAstManipulationNode rv)
        {
            var so = context.Configuration.WellKnownTypes.Object;
            rv = null;
            IXamlIlWrappedMethod FindAdderImpl(IXamlIlType targetType, IXamlIlType valueType, IXamlIlType keyType = null)
            {
                var candidates = targetType.FindMethods(m =>
                        !m.IsStatic && m.IsPublic
                                    && (m.Name == "Add" || m.Name.EndsWith(".Add"))).ToList();

                bool CheckArg(IXamlIlType argType, bool allowObj)
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
                            return new XamlIlWrappedMethod(m);
                        if (keyType != null && m.Parameters.Count == 2
                                                 && m.Parameters[0].IsAssignableFrom(keyType)
                                                 && CheckArg(m.Parameters[1], allowObj))
                            return new XamlIlWrappedMethod(m);

                    }
                }

                return null;
            }

            IXamlIlWrappedMethod FindAdderWithCast(IXamlIlType originalType, IXamlIlType newTargetType, IXamlIlType valueType)
            {
                var m = FindAdderImpl(newTargetType, valueType);
                if (m == null)
                    return null;
                return new XamlIlWrappedMethodWithCasts(m, new[] {originalType, m.ParametersWithThis[1]});

            }
            
            IXamlIlWrappedMethod FindAdder(IXamlIlType valueType, IXamlIlType keyType = null)
            {
                if(keyType == null)
                {
                    if (targetPropertyType.Equals(context.Configuration.WellKnownTypes.IEnumerable))
                        return FindAdderWithCast(targetPropertyType, context.Configuration.WellKnownTypes.IList,
                            valueType);
                    if (targetPropertyType.GenericTypeDefinition?.Equals(context.Configuration.WellKnownTypes
                            .IEnumerableT) == true)
                        return FindAdderWithCast(
                            targetPropertyType,
                            context.Configuration.WellKnownTypes.IListOfT
                                .MakeGenericType(targetPropertyType.GenericArguments[0]), valueType);
                }
                return FindAdderImpl(targetPropertyType, valueType, keyType);
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
                IXamlIlAstValueNode keyNode = null;

                bool IsKeyDirective(object node) => node is XamlIlAstXmlDirective d
                                                                        && d.Namespace == XamlNamespaces.Xaml2006 &&
                                                                        d.Name == "Key";

                void ProcessDirective(object d)
                {
                    var directive = (XamlIlAstXmlDirective) d;
                    if (directive.Values.Count != 1)
                        throw new XamlIlParseException("Invalid number of arguments for x:Key directive",
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
                
                IXamlIlAstManipulationNode VisitManipulationNode(IXamlIlAstManipulationNode man)
                {
                    if (IsKeyDirective(man))
                    {
                        ProcessDirective(man);
                        return new XamlIlManipulationGroupNode(man);
                    }
                    if(man is XamlIlManipulationGroupNode grp)
                        ProcessDirectiveCandidateList(grp.Children);
                    if (man is XamlIlObjectInitializationNode init)
                        init.Manipulation = VisitManipulationNode(init.Manipulation);
                    return man;
                }
                
                if (value is XamlIlAstObjectNode astObject)
                    ProcessDirectiveCandidateList(astObject.Children);
                else if (value is XamlIlValueWithManipulationNode vman)
                {
                    vman.Manipulation = VisitManipulationNode(vman.Manipulation);
                }
                    
                
                var adder = FindAdder(vtype, keyNode?.Type.GetClrType());
                if (adder != null)
                {
                    var args = new List<IXamlIlAstValueNode>();
                    if (keyNode != null)
                        args.Add(keyNode);
                    args.Add(value);
                    
                    rv = new XamlIlNoReturnMethodCallNode(value, adder, args);
                    if (targetProperty != null)
                        rv = new XamlIlPropertyValueManipulationNode(value, targetProperty, rv);
                    return true;
                }
            }
            
            return false;
        }

        public static IEnumerable<IXamlIlMethod> GetMarkupExtensionProvideValueAlternatives(
            XamlIlAstTransformationContext context,
            IXamlIlType type)
        {
            var sp = context.Configuration.TypeMappings.ServiceProvider;
            return type.FindMethods(m =>
                (m.Name == "ProvideValue" || m.Name == "ProvideTypedValue") && m.IsPublic && !m.IsStatic
                && (m.Parameters.Count == 0 || (m.Parameters.Count == 1 && m.Parameters[0].Equals(sp)))
            );

        }
        
        public static bool TryConvertMarkupExtension(XamlIlAstTransformationContext context,
            IXamlIlAstValueNode node, IXamlIlProperty prop, out XamlIlMarkupExtensionNode o)
        {
            o = null;
            var nodeType = node.Type.GetClrType();
            var candidates = GetMarkupExtensionProvideValueAlternatives(context, nodeType).ToList();
            var so = context.Configuration.WellKnownTypes.Object;
            var sp = context.Configuration.TypeMappings.ServiceProvider;
            
            // Try non-object variant first and variants without IServiceProvider argument first
            
            var provideValue = candidates.FirstOrDefault(m => m.Parameters.Count == 0 && !m.ReturnType.Equals(so))
                               ?? candidates.FirstOrDefault(m => m.Parameters.Count == 0)
                               ?? candidates.FirstOrDefault(m =>
                                   m.Parameters.Count == 1 && m.Parameters[0].Equals(sp) && !m.ReturnType.Equals(so))
                               ?? candidates.FirstOrDefault(m => m.Parameters.Count == 1 && m.Parameters[0].Equals(sp));

            if (provideValue == null)
            {
                if (node.Type.IsMarkupExtension)
                    throw new XamlIlParseException(
                        $"{node.Type.GetClrType().GetFqn()} was resolved as markup extension, but doesn't have a matching ProvideValue/ProvideTypedValue method",
                        node.Type);
                
                return false;
            }
            o = new XamlIlMarkupExtensionNode(node, prop, provideValue, node, null);
            return true;
        }

        public static bool TryGetCorrectlyTypedValue(XamlIlAstTransformationContext context,
            IXamlIlAstValueNode node, IXamlIlType type, out IXamlIlAstValueNode rv)
        {
            var cfg = context.Configuration;
            rv = null;
            if (type.IsAssignableFrom(node.Type.GetClrType()))
            {
                rv = node;
                return true;
            }

            
            // Since we are doing a conversion anyway, it makes sense to check for the underlying nullable type
            if (type.GenericTypeDefinition?.Equals(cfg.WellKnownTypes.NullableT) == true) 
                type = type.GenericArguments[0];
            
            
            if (cfg.CustomValueConverter?.Invoke(context, node, type, out rv) == true)
                return true;

            var nodeType = node.Type.GetClrType();
            // Implicit type converters
            if (!nodeType.Equals(cfg.WellKnownTypes.String))
                return false;


            if (node is XamlIlAstTextNode tn)
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
                    var resolvedType = XamlIlTypeReferenceResolver.ResolveType(context, tn.Text, false, tn, true);
                    rv = new XamlIlTypeExtensionNode(tn, resolvedType, type);
                    return true;
                }

                if (cfg.WellKnownTypes.Delegate.IsAssignableFrom(type))
                {
                    var invoke = type.FindMethod(m => m.Name == "Invoke");
                    var rootType = context.RootObject.Type.GetClrType();
                    var handler = 
                        rootType.FindMethod(tn.Text, invoke.ReturnType, false, invoke.Parameters.ToArray());
                    if (handler != null)
                    {
                        rv = new XamlIlLoadMethodDelegateNode(tn, context.RootObject, type, handler);
                        return true;
                    }
                }
            }

            IXamlIlAstValueNode CreateInvariantCulture() =>
                new XamlIlStaticOrTargetedReturnMethodCallNode(node,
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
                var args = new List<IXamlIlAstValueNode> {node};
                if (parser.Parameters.Count == 2)
                    args.Add(CreateInvariantCulture());

                rv = new XamlIlStaticOrTargetedReturnMethodCallNode(node, parser, args);
                return true;
            }

            if (cfg.TypeMappings.TypeDescriptorContext != null)
            {
                var typeConverterAttribute =
                    cfg.GetCustomAttribute(type, cfg.TypeMappings.TypeConverterAttributes).FirstOrDefault();
                if (typeConverterAttribute != null)
                {
                    var arg = typeConverterAttribute.Parameters.FirstOrDefault();
                    var converterType = (arg as IXamlIlType) ??
                                        (arg is String sarg ? cfg.TypeSystem.FindType(sarg) : null);
                    if (converterType != null)
                    {
                        var converterMethod = converterType.FindMethod("ConvertFrom", cfg.WellKnownTypes.Object, false,
                            cfg.TypeMappings.TypeDescriptorContext, cfg.WellKnownTypes.CultureInfo,
                            cfg.WellKnownTypes.Object);
                        rv = new XamlIlAstNeedsParentStackValueNode(node,
                            new XamlIlAstRuntimeCastNode(node,
                                new XamlIlStaticOrTargetedReturnMethodCallNode(node, converterMethod,
                                    new[]
                                    {
                                        new XamlIlAstNewClrObjectNode(node,
                                            new XamlIlAstClrTypeReference(node, converterType, false), null,
                                            new List<IXamlIlAstValueNode>()),
                                        new XamlIlAstContextLocalNode(node, cfg.TypeMappings.TypeDescriptorContext),
                                        CreateInvariantCulture(),
                                        node
                                    }), new XamlIlAstClrTypeReference(node, type, false)));
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
