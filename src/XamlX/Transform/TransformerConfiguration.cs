using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class TransformerConfiguration
    {
        private readonly Dictionary<Type, object> _extras = new();

        /// <summary>
        /// Gets extension configuration section
        /// </summary>
        public T GetExtra<T>() where T : notnull => (T) _extras[typeof(T)];

        /// <summary>
        /// Gets or create extension configuration section
        /// </summary>
        public T GetOrCreateExtra<T>()
            where T : notnull, new()
        {
            if (!_extras.TryGetValue(typeof(T), out var rv))
                _extras[typeof(T)] = rv = new T();
            return (T)rv;
        }

        /// <summary>
        /// Adds extension configuration section
        /// </summary>
        /// <param name="extra"></param>
        /// <typeparam name="T"></typeparam>
        public void AddExtra<T>(T extra) where T : notnull => _extras[typeof(T)] = extra;

        public delegate bool XamlValueConverter(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IReadOnlyList<IXamlCustomAttribute>? customAttributes,
            IXamlType type,
            [NotNullWhen(true)] out IXamlAstValueNode? result);
        
        public IXamlTypeSystem TypeSystem { get; }
        public IXamlAssembly? DefaultAssembly { get; }
        public XamlLanguageTypeMappings TypeMappings { get; }
        public XamlXmlnsMappings XmlnsMappings { get; }
        public XamlTypeWellKnownTypes WellKnownTypes { get; }
        public XamlValueConverter? CustomValueConverter { get; }
        public XamlDiagnosticsHandler DiagnosticsHandler { get; }
        public IXamlIdentifierGenerator IdentifierGenerator { get; }
        public List<(string ns, string name)> KnownDirectives { get; } = [];

        public TransformerConfiguration(
            IXamlTypeSystem typeSystem,
            IXamlAssembly? defaultAssembly,
            XamlLanguageTypeMappings typeMappings,
            XamlXmlnsMappings? xmlnsMappings = null,
            XamlValueConverter? customValueConverter = null,
            IXamlIdentifierGenerator? identifierGenerator = null,
            XamlDiagnosticsHandler? diagnosticsHandler = null)
        {
            TypeSystem = typeSystem;
            DefaultAssembly = defaultAssembly;
            TypeMappings = typeMappings;
            XmlnsMappings = xmlnsMappings ?? XamlXmlnsMappings.Resolve(typeSystem, typeMappings);
            WellKnownTypes = new XamlTypeWellKnownTypes(typeSystem);
            CustomValueConverter = customValueConverter;
            DiagnosticsHandler = diagnosticsHandler ?? new XamlDiagnosticsHandler();
            IdentifierGenerator = identifierGenerator ?? new GuidIdentifierGenerator();
        }

        private readonly Dictionary<object, IXamlProperty?> _contentPropertyCache = new();

        public IXamlProperty? FindContentProperty(IXamlType type)
        {
            if (TypeMappings.ContentAttributes.Count == 0)
                return null;
            if (_contentPropertyCache.TryGetValue(type.Id, out var found))
                return found;

            foreach (var contentAttributeOnType in GetCustomAttribute(type, TypeMappings.ContentAttributes))
            {
                if (contentAttributeOnType.Properties.Count == 0)
                {
                    throw new XamlTypeSystemException($"The '{contentAttributeOnType.Type}' attribute must have a property name specified");
                }
                if (contentAttributeOnType.Properties["Name"] is string propertyName)
                {
                    var contentProperty = type.GetAllProperties().FirstOrDefault(prop => prop.Name == propertyName);
                    if (contentProperty is null)
                    {
                        throw new XamlTypeSystemException($"The property name '{propertyName}' specified in the content property of {type.GetFqn()} does not exist.");
                    }

                    if (found != null && !contentProperty.Equals(found))
                        throw new XamlTypeSystemException(
                            "Content attribute is declared on multiple properties of " + type.GetFqn());
                    found = contentProperty;
                }
            }
            
            foreach (var p in type.Properties)
            {
                if (GetCustomAttribute(p, TypeMappings.ContentAttributes).Any())
                {
                    if (found != null && !p.Equals(found))
                        throw new XamlTypeSystemException(
                            "Content attribute is declared on multiple properties of " + type.GetFqn());
                    found = p;
                }
            }

            // Fall back to a Content attribute found on a base type
            if ((found == null) && (type.BaseType != null))
                found = FindContentProperty(type.BaseType);

            return _contentPropertyCache[type.Id] = found;
        }

        private readonly IDictionary<object, bool> _whitespaceSignificantCollectionCache = new Dictionary<object, bool>();

        /// <summary>
        /// Checks whether the given type is annotated as a collection that treats whitespace as significant.
        /// </summary>
        public bool IsWhitespaceSignificantCollection(IXamlType type)
        {
            return IsAttributePresentInTypeHierarchy(
                type,
                TypeMappings.WhitespaceSignificantCollectionAttributes,
                _whitespaceSignificantCollectionCache
            );
        }

        private readonly IDictionary<object, bool> _trimSurroundingWhitespaceCache = new Dictionary<object, bool>();

        /// <summary>
        /// Checks whether the given type is annotated to indicate that surrounding whitespace should be trimmed
        /// even if it is contained in a <see cref="IsWhitespaceSignificantCollection(XamlX.TypeSystem.IXamlType)">
        /// whitespace-significant collection</see>. Note that this behavior is diabled in an xml:space="preserve"
        /// scope.
        /// </summary>
        public bool IsTrimSurroundingWhitespaceElement(IXamlType type)
        {
            return IsAttributePresentInTypeHierarchy(
                type,
                TypeMappings.TrimSurroundingWhitespaceAttributes,
                _trimSurroundingWhitespaceCache
            );
        }

        private bool IsAttributePresentInTypeHierarchy(IXamlType type, List<IXamlType> attributes, IDictionary<object, bool> cache)
        {
            if (attributes.Count == 0)
                return false;
            if (cache.TryGetValue(type.Id, out var result))
                return result;

            // Check the base type first
            if (type.BaseType != null)
                result = IsAttributePresentInTypeHierarchy(type.BaseType, attributes, cache);

            // Check if the current type has any of the configured attributes
            if (!result)
            {
                result = GetCustomAttribute(type, attributes).Any();
            }

            return cache[type.Id] = result;
        }

        public IEnumerable<IXamlCustomAttribute> GetCustomAttribute(IXamlType type, IXamlType attributeType)
        {
            var custom = TypeMappings.CustomAttributeResolver?.GetCustomAttribute(type, attributeType);
            if (custom != null)
                yield return custom;
            foreach(var attr in type.CustomAttributes)
                if (attr.Type.Equals(attributeType))
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
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlTypeWellKnownTypes
    {
        public IXamlType IList { get; }
        public IXamlType IEnumerable { get; }
        public IXamlType IEnumerableT { get; }
        public IXamlType IListOfT { get; }
        public IXamlType Object { get; }
        public IXamlType String { get; }
        public IXamlType Int32 { get; }
        public IXamlType Void { get; }
        public IXamlType Boolean { get; }
        public IXamlType Double { get; }
        public IXamlType NullableT { get; }
        public IXamlType CultureInfo { get; }
        public IXamlType IFormatProvider { get; }
        public IXamlType Delegate { get; }
        public IXamlType ObsoleteAttribute { get; }

        public XamlTypeWellKnownTypes(IXamlTypeSystem typeSystem)
        {
            Void = typeSystem.GetType("System.Void");
            String = typeSystem.GetType("System.String");
            Int32 = typeSystem.GetType("System.Int32");
            Object = typeSystem.GetType("System.Object");
            Boolean = typeSystem.GetType("System.Boolean");
            Double = typeSystem.GetType("System.Double");
            CultureInfo = typeSystem.GetType("System.Globalization.CultureInfo");
            IFormatProvider = typeSystem.GetType("System.IFormatProvider");
            IList = typeSystem.GetType("System.Collections.IList");
            IEnumerable = typeSystem.GetType("System.Collections.IEnumerable");
            IListOfT = typeSystem.GetType("System.Collections.Generic.IList`1");
            IEnumerableT = typeSystem.GetType("System.Collections.Generic.IEnumerable`1");
            NullableT = typeSystem.GetType("System.Nullable`1");
            Delegate = typeSystem.GetType("System.Delegate");
            ObsoleteAttribute = typeSystem.GetType("System.ObsoleteAttribute");
        }
    }
}
