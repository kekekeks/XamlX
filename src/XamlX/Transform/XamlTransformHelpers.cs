using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    static class XamlTransformHelpers
    {
        class AdderCache : Dictionary<IXamlType, IReadOnlyList<IXamlMethod>>
        {
            
        }

        public static IReadOnlyList<IXamlMethod> FindPossibleAdders(AstTransformationContext context,
            IXamlType type)
        {
            IReadOnlyList<IXamlMethod> FindPossibleAddersImpl()
            {
                var known = context.Configuration.WellKnownTypes;

                // Attempt to cast IEnumerable and IEnumerable<T> to IList<T>
                var actualType = type;
                if (actualType.Equals(known.IEnumerable))
                    actualType = known.IList;
                if (actualType.GenericTypeDefinition?.Equals(known.IEnumerableT) == true)
                    actualType = known.IListOfT.MakeGenericType(actualType.GenericArguments[0]);

                var inspectTypes = new List<IXamlType>();
                inspectTypes.Add(actualType);
                inspectTypes.AddRange(actualType.GetAllInterfaces());

                // If type supports IList<T> don't fall back to IList
                if (inspectTypes.Any(t => t.GenericTypeDefinition?.Equals(known.IListOfT) == true))
                    inspectTypes = inspectTypes.Where(t => !t.Equals(known.IList)).ToList();

                var rv = new List<IXamlMethod>();
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

                if(context.Configuration.TypeMappings.IAddChildOfT != null)
                {
                    var addChildT = inspectTypes.Where(t => t.GenericTypeDefinition?.Equals(context.Configuration.TypeMappings.IAddChildOfT) == true);

                    foreach (var t in addChildT)
                    {
                        var adder = t.GetMethod(x => x.Name == "AddChild");

                        rv.Add(adder);
                    }
                }

                if(context.Configuration.TypeMappings.IAddChild != null)
                {
                    var addChild = inspectTypes.SingleOrDefault(t => t.Equals(context.Configuration.TypeMappings.IAddChild) == true);

                    if (addChild != null)
                    {
                        var adder = addChild.GetMethod(x => x.Name == "AddChild");

                        rv.Add(adder);
                    }
                }              

                return rv;
            }
            
            var cache = context.GetOrCreateItem<AdderCache>();
            if (cache.TryGetValue(type, out var rvr))
                return rvr;
            else
                return cache[type] = FindPossibleAddersImpl();


        }


        public static IEnumerable<IXamlMethod> GetMarkupExtensionProvideValueAlternatives(
            AstTransformationContext context,
            IXamlType type)
        {
            var sp = context.Configuration.TypeMappings.ServiceProvider;
            return type.FindMethods(m =>
                (m.Name == "ProvideValue" || m.Name == "ProvideTypedValue") && m.IsPublic && !m.IsStatic
                && (m.Parameters.Count == 0 || (m.Parameters.Count == 1 && m.Parameters[0].Equals(sp)))
            );
        }

        class MarkupExtensionProvideValueCache
        {
            public Dictionary<IXamlType, IXamlMethod?> TypeToProvideValue = new();
        }

        public static bool TryConvertMarkupExtension(
            AstTransformationContext context,
            IXamlAstValueNode node,
            [NotNullWhen(true)] out XamlMarkupExtensionNode? o)
        {
            var cache = context.GetOrCreateItem<MarkupExtensionProvideValueCache>();
            o = null;
            var nodeType = node.Type.GetClrType();

            if (!cache.TypeToProvideValue.TryGetValue(nodeType, out var provideValue))
            {
                var candidates = GetMarkupExtensionProvideValueAlternatives(context, nodeType).ToList();
                var so = context.Configuration.WellKnownTypes.Object;
                var sp = context.Configuration.TypeMappings.ServiceProvider;

                // Try non-object variant first and variants without IServiceProvider argument first

                provideValue = candidates.FirstOrDefault(m => m.Parameters.Count == 0 && !m.ReturnType.Equals(so))
                               ?? candidates.FirstOrDefault(m => m.Parameters.Count == 0)
                               ?? candidates.FirstOrDefault(m =>
                                   m.Parameters.Count == 1 && m.Parameters[0].Equals(sp) &&
                                   !m.ReturnType.Equals(so))
                               ?? candidates.FirstOrDefault(m =>
                                   m.Parameters.Count == 1 && m.Parameters[0].Equals(sp));
                cache.TypeToProvideValue[nodeType] = provideValue;
            }

            if (provideValue == null)
            {
                if (node.Type.IsMarkupExtension)
                    context.ReportTransformError(
                        $"{nodeType.GetFqn()} was resolved as markup extension, but doesn't have a matching ProvideValue/ProvideTypedValue method",
                        node);
                
                return false;
            }
            o = new XamlMarkupExtensionNode(node, provideValue, node);
            return true;
        }

        public static bool TryGetCorrectlyTypedValue(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IXamlType xamlType,
            [NotNullWhen(true)] out IXamlAstValueNode? rv)
        {
            return TryGetCorrectlyTypedValue(context, node, null, xamlType, out rv);
        }

        public static bool TryGetCorrectlyTypedValue(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IXamlProperty property,
            [NotNullWhen(true)] out IXamlAstValueNode? rv)
        {
            return TryGetCorrectlyTypedValue(context, node, property.CustomAttributes, property.PropertyType, out rv);
        }

        public static bool TryGetCorrectlyTypedValue(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IXamlParameterInfo parameterInfo,
            [NotNullWhen(true)] out IXamlAstValueNode? rv)
        {
            return TryGetCorrectlyTypedValue(context, node, parameterInfo.CustomAttributes, parameterInfo.ParameterType, out rv);
        }

        public static bool TryGetCorrectlyTypedValue(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IReadOnlyList<IXamlCustomAttribute>? customAttributes,
            IXamlType type,
            [NotNullWhen(true)] out IXamlAstValueNode? rv)
        {
            if (type.IsAssignableFrom(node.Type.GetClrType()))
            {
                rv = node;
                return true;
            }

            return TryConvertValue(context, node, customAttributes, type, null, out rv);
        }

        public static IXamlType? TryGetTypeConverterFromCustomAttribute(
            TransformerConfiguration cfg,
            IXamlCustomAttribute? attribute)
        {

            if (attribute != null)
            {
                var arg = attribute.Parameters.FirstOrDefault();
                return (arg as IXamlType) ??
                                    (arg is String sarg ? cfg.TypeSystem.FindType(sarg) : null);
                
            }

            return null;
        }

        public static IXamlType GetCommonBaseClass(this IXamlType[] types)
        {
            if (types is null)
                throw new ArgumentNullException(nameof(types));
            if (types.Length == 0)
                throw new ArgumentException("Input types array must not be empty", nameof(types));

            var ret = types[0];

            for (var i = 1; i < types.Length; ++i)
            {
                if (types[i].IsAssignableFrom(ret))
                {
                    ret = types[i];
                }
                else
                {
                    // This will always terminate when ret == typeof(object)
                    while (!ret!.IsAssignableFrom(types[i]))
                        ret = ret.BaseType;
                }
            }

            return ret;
        }

        static IXamlAstValueNode CreateInvariantCulture(TransformerConfiguration cfg, IXamlLineInfo lineInfo) =>
            new XamlStaticOrTargetedReturnMethodCallNode(lineInfo,
                cfg.WellKnownTypes.CultureInfo.Methods.First(x =>
                    x.IsPublic && x.IsStatic && x.Name == "get_InvariantCulture"), null);

        public static bool TryConvertValue(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IReadOnlyList<IXamlCustomAttribute>? customAttributes,
            IXamlType type,
            XamlAstClrProperty? propertyContext,
            [NotNullWhen(true)] out IXamlAstValueNode? rv)
        {    
            rv = null;
            var cfg = context.Configuration;
            // Since we are doing a conversion anyway, it makes sense to check for the underlying nullable type
            if (type.GenericTypeDefinition?.Equals(cfg.WellKnownTypes.NullableT) == true) 
                type = type.GenericArguments[0];

            var nodeType = node.Type.GetClrType();

            // Try with property-defined converter first
            if (propertyContext?.TypeConverters.TryGetValue(type, out var propertyConverterType) == true)
            {
                rv = ConvertWithConverter(node, propertyConverterType, cfg, type);
                return true;
            }

            // Ask the hosting platform to apply its custom conversions
            var attrs = customAttributes?.Any() == true ? customAttributes : propertyContext?.CustomAttributes;
            if (cfg.CustomValueConverter?.Invoke(context, node, attrs, type, out rv) == true)
                return true;

            // Implicit type converters
            if (!nodeType.Equals(cfg.WellKnownTypes.String))
                return false;

            if (node is XamlAstTextNode tn)
            {
                if (type.IsEnum)
                {
                    if (TypeSystemHelpers.TryGetEnumValueNode(type, tn.Text, tn, false, out var enumConstantNode))
                    {
                        rv = enumConstantNode;
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
                    var resolvedType = TypeReferenceResolver.ResolveType(context, tn.Text, false, tn, true);
                    rv = new XamlTypeExtensionNode(tn, resolvedType, type);
                    return true;
                }

                if (cfg.WellKnownTypes.Delegate.IsAssignableFrom(type))
                {
                    var invoke = type.GetMethod(m => m.Name == "Invoke");
                    var rootType = context.RootObject.Type.GetClrType();
                    var handler = 
                        rootType.FindMethod(tn.Text, invoke.ReturnType, false, invoke.Parameters.ToArray());
                    if (handler != null)
                    {
                        rv = new XamlLoadMethodDelegateNode(tn, context.RootObject, type, handler);
                        return true;
                    }
                }
            }

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
                var args = new List<IXamlAstValueNode> {node};
                if (parser.Parameters.Count == 2)
                    args.Add(CreateInvariantCulture(cfg, node));

                rv = new XamlStaticOrTargetedReturnMethodCallNode(node, parser, args);
                return true;
            }

            if (cfg.TypeMappings.TypeDescriptorContext != null)
            {
                var typeConverterAttribute =
                    cfg.GetCustomAttribute(type, cfg.TypeMappings.TypeConverterAttributes).FirstOrDefault();
                if (typeConverterAttribute != null)
                {
                    var converterType = TryGetTypeConverterFromCustomAttribute(cfg, typeConverterAttribute);
                    if (converterType is not null)
                    {
                        rv = ConvertWithConverter(node, converterType, cfg, type);
                        return true;
                    }
                }
            }

            return false;
        }

        private static IXamlAstValueNode ConvertWithConverter(IXamlAstValueNode node, IXamlType converterType,
            TransformerConfiguration cfg, IXamlType type)
        {
            Debug.Assert(cfg.TypeMappings.TypeDescriptorContext is not null);
            var typeDescriptorContext = cfg.TypeMappings.TypeDescriptorContext!;

            var converterMethod = converterType.GetMethod("ConvertFrom", cfg.WellKnownTypes.Object, false,
                typeDescriptorContext, cfg.WellKnownTypes.CultureInfo,
                cfg.WellKnownTypes.Object);

            var rv = new XamlAstNeedsParentStackValueNode(node,
                new XamlAstRuntimeCastNode(node,
                    new XamlStaticOrTargetedReturnMethodCallNode(node, converterMethod,
                        new[]
                        {
                            new XamlAstNewClrObjectNode(node,
                                new XamlAstClrTypeReference(node, converterType, false), converterType.GetConstructor(),
                                new List<IXamlAstValueNode>()),
                            new XamlAstContextLocalNode(node, typeDescriptorContext),
                            CreateInvariantCulture(cfg, node), node
                        }), new XamlAstClrTypeReference(node, type, false)));
            return rv;
        }
    }
}
