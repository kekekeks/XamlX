using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLIL_INTERNAL
    public
#endif
    static class XamlXTransformHelpers
    {
        /*
        public static void GeneratePropertyAssignments(XamlXAstTransformationContext context,
            XamlXAstClrProperty contentProperty,
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
            XamlXAstClrProperty property, List<IXamlXAstValueNode> nodes)
        {
            var tmp = nodes.Cast<IXamlXAstNode>().ToList();
            GeneratePropertyAssignments(context, property, tmp.Count,
                i => (IXamlXAstValueNode) tmp[i],
                (i, v) => tmp[i] = v);
            return tmp.Cast<IXamlXAstManipulationNode>().ToList();
        }*/

        class AdderCache : Dictionary<IXamlXType, IReadOnlyList<IXamlXMethod>>
        {
            
        }

        public static IReadOnlyList<IXamlXMethod> FindPossibleAdders(XamlXAstTransformationContext context,
            IXamlXType type)
        {
            IReadOnlyList<IXamlXMethod> FindPossibleAddersImpl()
            {
                var known = context.Configuration.WellKnownTypes;

                // Attempt to cast IEnumerable and IEnumerable<T> to IList<T>
                var actualType = type;
                if (actualType.Equals(known.IEnumerable))
                    actualType = known.IList;
                if (actualType.GenericTypeDefinition?.Equals(known.IEnumerableT) == true)
                    actualType = known.IListOfT.MakeGenericType(actualType.GenericArguments[0]);

                var inspectTypes = new List<IXamlXType>();
                inspectTypes.Add(actualType);
                inspectTypes.AddRange(actualType.GetAllInterfaces());

                // If type supports IList<T> don't fall back to IList
                if (inspectTypes.Any(t => t.GenericTypeDefinition?.Equals(known.IListOfT) == true))
                    inspectTypes = inspectTypes.Where(t => !t.Equals(known.IList)).ToList();

                var rv = new List<IXamlXMethod>();
                foreach (var t in inspectTypes)
                {
                    foreach (var m in t.FindMethods(m => m.Name == "Add" && m.IsPublic && !m.IsStatic
                                                         && (m.Parameters.Count == 1 || m.Parameters.Count == 2)))
                    {
                        if (rv.Any(em => em.Equals(m)))
                            continue;
                        rv.Add(m);
                    }
                }
                
                // First use methods from the type itself, then from base types, then from interfaces
                rv = rv
                    .OrderByDescending(x => x.ThisOrFirstParameter().Equals(actualType))
                    .ThenBy(x => x.ThisOrFirstParameter().IsInterface)
                    .ToList();
                
                // Add casts
                for (var c = 0; c < rv.Count; c++)
                    if (!rv[c].ThisOrFirstParameter().Equals(type))
                        rv[c] = new XamlXMethodWithCasts(rv[c], new[] {type}.Concat(rv[c].Parameters));

                return rv;
            }
            
            var cache = context.GetOrCreateItem<AdderCache>();
            if (cache.TryGetValue(type, out var rvr))
                return rvr;
            else
                return cache[type] = FindPossibleAddersImpl();


        }


        public static IEnumerable<IXamlXMethod> GetMarkupExtensionProvideValueAlternatives(
            XamlXAstTransformationContext context,
            IXamlXType type)
        {
            var sp = context.Configuration.TypeMappings.ServiceProvider;
            return type.FindMethods(m =>
                (m.Name == "ProvideValue" || m.Name == "ProvideTypedValue") && m.IsPublic && !m.IsStatic
                && (m.Parameters.Count == 0 || (m.Parameters.Count == 1 && m.Parameters[0].Equals(sp)))
            );
        }
        
        public static bool TryConvertMarkupExtension(XamlXAstTransformationContext context,
            IXamlXAstValueNode node, out XamlXMarkupExtensionNode o)
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
                    throw new XamlXParseException(
                        $"{node.Type.GetClrType().GetFqn()} was resolved as markup extension, but doesn't have a matching ProvideValue/ProvideTypedValue method",
                        node.Type);
                
                return false;
            }
            o = new XamlXMarkupExtensionNode(node, provideValue, node);
            return true;
        }

        public static bool TryGetCorrectlyTypedValue(XamlXAstTransformationContext context,
            IXamlXAstValueNode node, IXamlXType type, out IXamlXAstValueNode rv)
        {
            if (type.IsAssignableFrom(node.Type.GetClrType()))
            {
                rv = node;
                return true;
            }

            return TryConvertValue(context, node, type, out rv);
        }

        public static bool TryConvertValue(XamlXAstTransformationContext context,
                IXamlXAstValueNode node, IXamlXType type, out IXamlXAstValueNode rv)
        {    
            rv = null;
            var cfg = context.Configuration;
            // Since we are doing a conversion anyway, it makes sense to check for the underlying nullable type
            if (type.GenericTypeDefinition?.Equals(cfg.WellKnownTypes.NullableT) == true) 
                type = type.GenericArguments[0];
            
            
            if (cfg.CustomValueConverter?.Invoke(context, node, type, out rv) == true)
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
                    if (enumValue == null && !string.IsNullOrWhiteSpace(tn.Text) &&
                        long.TryParse(tn.Text, out var parsed))
                    {
                        var enumTypeName = type.GetEnumUnderlyingType().Name;
                        var obj = enumTypeName == "Int32" || enumTypeName == "UInt32" ?
                            unchecked((int)parsed) :
                            (object)parsed;
                        rv = new XamlXConstantNode(node, type, obj);
                        return true;
                    }
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
                    var resolvedType = XamlXTypeReferenceResolver.ResolveType(context, tn.Text, false, tn, true);
                    rv = new XamlXTypeExtensionNode(tn, resolvedType, type);
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
                        rv = new XamlXLoadMethodDelegateNode(tn, context.RootObject, type, handler);
                        return true;
                    }
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
                        rv = new XamlXAstNeedsParentStackValueNode(node,
                            new XamlXAstRuntimeCastNode(node,
                                new XamlXStaticOrTargetedReturnMethodCallNode(node, converterMethod,
                                    new[]
                                    {
                                        new XamlXAstNewClrObjectNode(node,
                                            new XamlXAstClrTypeReference(node, converterType, false), null,
                                            new List<IXamlXAstValueNode>()),
                                        new XamlXAstContextLocalNode(node, cfg.TypeMappings.TypeDescriptorContext),
                                        CreateInvariantCulture(),
                                        node
                                    }), new XamlXAstClrTypeReference(node, type, false)));
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
