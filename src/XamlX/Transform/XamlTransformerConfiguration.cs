using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlTransformerConfiguration
    {
        private Dictionary<Type, object> _extras = new Dictionary<Type, object>();
        /// <summary>
        /// Gets extension configuration section
        /// </summary>
        public T GetExtra<T>() => (T) _extras[typeof(T)];
        /// <summary>
        /// Adds extension configuration section
        /// </summary>
        /// <param name="extra"></param>
        /// <typeparam name="T"></typeparam>
        public void AddExtra<T>(T extra) => _extras[typeof(T)] = extra;

        public delegate bool XamlValueConverter(IXamlAstValueNode node, IXamlType type, out IXamlAstValueNode result);
        
        public IXamlTypeSystem TypeSystem { get; }
        public IXamlAssembly DefaultAssembly { get; }
        public XamlLanguageTypeMappings TypeMappings { get; }
        public XamlXmlnsMappings XmlnsMappings { get; }
        public XamlTypeWellKnownTypes WellKnownTypes { get; }
        public XamlValueConverter CustomValueConverter { get; }
        public List<(string ns, string name)> KnownDirectives { get; } = new List<(string, string)>
        {
            (XamlNamespaces.Xaml2006, "Arguments")
        };

        public XamlTransformerConfiguration(IXamlTypeSystem typeSystem, IXamlAssembly defaultAssembly,
            XamlLanguageTypeMappings typeMappings, XamlXmlnsMappings xmlnsMappings = null,
            XamlValueConverter customValueConverter = null)
        {
            TypeSystem = typeSystem;
            DefaultAssembly = defaultAssembly;
            TypeMappings = typeMappings;
            XmlnsMappings = xmlnsMappings ?? XamlXmlnsMappings.Resolve(typeSystem, typeMappings);
            WellKnownTypes = new XamlTypeWellKnownTypes(typeSystem);
            CustomValueConverter = customValueConverter;
        }

        IDictionary<object, IXamlProperty> _contentPropertyCache = new Dictionary<object, IXamlProperty>();

        public IXamlProperty FindContentProperty(IXamlType type)
        {
            if (TypeMappings.ContentAttributes.Count == 0)
                return null;
            if (_contentPropertyCache.TryGetValue(type.Id, out var found))
                return found;

            // Check the base type first, we'll need to throw on duplicate Content property later
            if (type.BaseType != null)
                found = FindContentProperty(type.BaseType);
            
            foreach (var p in type.Properties)
            {
                if (GetCustomAttribute(p, TypeMappings.ContentAttributes).Any())
                {
                    if (found != null && !p.Equals(found))
                        throw new XamlTypeSystemException(
                            "Content (or substitute) attribute is declared on multiple properties of " + type.GetFqn());
                    found = p;
                }
            }

            return _contentPropertyCache[type.Id] = found;
        }
        

        public IEnumerable<IXamlCustomAttribute> GetCustomAttribute(IXamlType type, IXamlType attributeType)
        {
            var custom = TypeMappings.CustomAttributeResolver?.GetCustomAttribute(type, attributeType);
            if (custom != null)
                yield return custom;
            foreach(var attr in type.CustomAttributes)
                if (attr.Type == attributeType)
                    yield return attr;
        }
        
        public IEnumerable<IXamlCustomAttribute> GetCustomAttribute(IXamlType type, IEnumerable<IXamlType> types)
        {
            foreach(var t in types)
            foreach (var a in GetCustomAttribute(type, t))
                yield return a;
        }
        
        
        public IEnumerable<IXamlCustomAttribute> GetCustomAttribute(IXamlProperty prop, IXamlType attributeType)
        {
            var custom = TypeMappings.CustomAttributeResolver?.GetCustomAttribute(prop, attributeType);
            if (custom != null)
                yield return custom;
            foreach(var attr in prop.CustomAttributes)
                if (attr.Type.Equals(attributeType))
                    yield return attr;
        }
        
        public IEnumerable<IXamlCustomAttribute> GetCustomAttribute(IXamlProperty prop, IEnumerable<IXamlType> types)
        {
            foreach(var t in types)
            foreach (var a in GetCustomAttribute(prop, t))
                yield return a;
        }
       
        public bool TryGetCorrectlyTypedValue(IXamlAstValueNode node, IXamlType type, out IXamlAstValueNode rv)
        {
            rv = null;
            if (type.IsAssignableFrom(node.Type.GetClrType()))
            {
                rv = node;
                return true;
            }

            if (CustomValueConverter?.Invoke(node, type, out rv) == true)
                return true;

            var nodeType = node.Type.GetClrType();
            // Implicit type converters
            if (!nodeType.Equals(WellKnownTypes.String))
                return false;

            var candidates = type.Methods.Where(m => m.Name == "Parse"
                                                     && m.ReturnType.Equals(type)
                                                     && m.Parameters.Count > 0
                                                     && m.Parameters[0].Equals(WellKnownTypes.String)).ToList();

            // Well known types
            if (node is XamlAstTextNode tn &&
                TypeSystemHelpers.ParseConstantIfTypeAllows(tn.Text, type, tn, out var constantNode))
            {
                rv = constantNode;
                return true;
            }

            IXamlAstValueNode CreateInvariantCulture() =>
                new XamlStaticOrTargetedReturnMethodCallNode(node,
                    WellKnownTypes.CultureInfo.Methods.First(x =>
                        x.IsPublic && x.IsStatic && x.Name == "get_InvariantCulture"), null);
            
            // Types with parse method
            var parser = candidates.FirstOrDefault(m =>
                             m.Parameters.Count == 2 &&
                             (
                                 m.Parameters[1].Equals(WellKnownTypes.CultureInfo)
                                 || m.Parameters[1].Equals(WellKnownTypes.IFormatProvider)
                             )
                         )
                         ?? candidates.FirstOrDefault(m => m.Parameters.Count == 1);
            if (parser != null)
            {
                var args = new List<IXamlAstValueNode> {node};
                if (parser.Parameters.Count == 2) 
                    args.Add(CreateInvariantCulture());

                rv = new XamlStaticOrTargetedReturnMethodCallNode(node, parser, args);
                return true;
            }

            if (TypeMappings.TypeDescriptorContext != null)
            {
                var typeConverterAttribute =
                    GetCustomAttribute(type, TypeMappings.TypeConverterAttributes).FirstOrDefault();
                if (typeConverterAttribute != null)
                {
                    var arg = typeConverterAttribute.Parameters.FirstOrDefault();
                    var converterType = (arg as IXamlType) ?? (arg is String sarg ? TypeSystem.FindType(sarg) : null);
                    if (converterType != null)
                    {
                        var converterMethod = converterType.FindMethod("ConvertFrom", WellKnownTypes.Object, false,
                            TypeMappings.TypeDescriptorContext, WellKnownTypes.CultureInfo, WellKnownTypes.Object);
                        rv =
                            new XamlAstRuntimeCastNode(node,
                                new XamlStaticOrTargetedReturnMethodCallNode(node, converterMethod,
                                    new[]
                                    {
                                        new XamlAstNewClrObjectNode(node,
                                            new XamlAstClrTypeReference(node, converterType),
                                            new List<IXamlAstValueNode>()),
                                        new XamlAstContextLocalNode(node, TypeMappings.TypeDescriptorContext),
                                        CreateInvariantCulture(),
                                        node
                                    }), new XamlAstClrTypeReference(node, type));
                        return true;
                    }
                }
            }

            //TODO: TypeConverter's
            return false;
        }
    }

    public class XamlTypeWellKnownTypes
    {
        public IXamlType IList { get; }
        public IXamlType IListOfT { get; }
        public IXamlType Object { get; }
        public IXamlType String { get; }
        public IXamlType Void { get; }
        public IXamlType Boolean { get; }
        public IXamlType NullableT { get; }
        public IXamlType CultureInfo { get; }
        public IXamlType IFormatProvider { get; }

        public XamlTypeWellKnownTypes(IXamlTypeSystem typeSystem)
        {
            Void = typeSystem.GetType("System.Void");
            String = typeSystem.GetType("System.String");
            Object = typeSystem.GetType("System.Object");
            Boolean = typeSystem.GetType("System.Boolean");
            CultureInfo = typeSystem.GetType("System.Globalization.CultureInfo");
            IFormatProvider = typeSystem.GetType("System.IFormatProvider");
            IList = typeSystem.GetType("System.Collections.IList");
            IListOfT = typeSystem.GetType("System.Collections.Generic.IList`1");
            NullableT = typeSystem.GetType("System.Nullable`1");
        }
    }
}