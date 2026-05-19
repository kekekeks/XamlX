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
        private readonly IXamlType[] _actionOfT;
        private readonly IXamlType[] _funcOfT;

        public IXamlType Action { get; }
        public IXamlType Boolean { get; }
        public IXamlType CultureInfo { get; }
        public IXamlType Delegate { get; }
        public IXamlType DictionaryOfT2 { get; }
        public IXamlType Double { get; }
        public IXamlType IEnumerable { get; }
        public IXamlType IEnumerableOfT { get; }
        public IXamlType IFormatProvider { get; }
        public IXamlType IList { get; }
        public IXamlType IListOfT { get; }
        public IXamlType IReadOnlyListOfT { get; }
        public IXamlType Int32 { get; }
        public IXamlType IntPtr { get; }
        public IXamlType InvalidCastException { get; }
        public IXamlType ListOfT { get; }
        public IXamlType MethodInfo { get; }
        public IXamlType NullReferenceException { get; }
        public IXamlType NullableT { get; }
        public IXamlType Object { get; }
        public IXamlType ObsoleteAttribute { get; }
        public IXamlType String { get; }
        public IXamlType Type { get; }
        public IXamlType Void { get; }
        public IXamlType? ExperimentalAttribute { get; }

        public IXamlType GetActionOfT(int typeParamCount)
            => _actionOfT[typeParamCount - 1];

        public IXamlType GetFuncOfT(int typeParamCount)
            => _funcOfT[typeParamCount - 1];

        [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = TrimmingMessages.TypeInCoreAssembly)]
        [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypeInCoreAssembly)]
        public XamlTypeWellKnownTypes(IXamlTypeSystem typeSystem)
        {
            Action = typeSystem.GetType("System.Action");
            Boolean = typeSystem.GetType("System.Boolean");
            CultureInfo = typeSystem.GetType("System.Globalization.CultureInfo");
            Delegate = typeSystem.GetType("System.Delegate");
            DictionaryOfT2 = typeSystem.GetType("System.Collections.Generic.Dictionary`2");
            Double = typeSystem.GetType("System.Double");
            IEnumerable = typeSystem.GetType("System.Collections.IEnumerable");
            IEnumerableOfT = typeSystem.GetType("System.Collections.Generic.IEnumerable`1");
            IFormatProvider = typeSystem.GetType("System.IFormatProvider");
            IList = typeSystem.GetType("System.Collections.IList");
            IListOfT = typeSystem.GetType("System.Collections.Generic.IList`1");
            IReadOnlyListOfT = typeSystem.GetType("System.Collections.Generic.IReadOnlyList`1");
            Int32 = typeSystem.GetType("System.Int32");
            IntPtr = typeSystem.GetType("System.IntPtr");
            InvalidCastException = typeSystem.GetType("System.InvalidCastException");
            ListOfT = typeSystem.GetType("System.Collections.Generic.List`1");
            MethodInfo = typeSystem.GetType("System.Reflection.MethodInfo");
            NullReferenceException = typeSystem.GetType("System.NullReferenceException");
            NullableT = typeSystem.GetType("System.Nullable`1");
            Object = typeSystem.GetType("System.Object");
            ObsoleteAttribute = typeSystem.GetType("System.ObsoleteAttribute");
            String = typeSystem.GetType("System.String");
            Type = typeSystem.GetType("System.Type");
            Void = typeSystem.GetType("System.Void");

            _actionOfT = new IXamlType[16];
            for (var i = 1; i <= 16; ++i)
                _actionOfT[i - 1] = typeSystem.GetType($"System.Action`{i}");

            _funcOfT = new IXamlType[17];
            for (var i = 1; i <= 17; ++i)
                _funcOfT[i - 1] = typeSystem.GetType($"System.Func`{i}");

            ExperimentalAttribute = typeSystem.FindType("System.Diagnostics.CodeAnalysis.ExperimentalAttribute");
        }
    }
}
