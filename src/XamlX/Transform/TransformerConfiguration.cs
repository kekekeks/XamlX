using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using XamlX.Ast;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class TransformerConfiguration
    {
        private Dictionary<Type, object> _extras = new Dictionary<Type, object>();
        /// <summary>
        /// Gets extension configuration section
        /// </summary>
        public T GetExtra<T>() => (T) _extras[typeof(T)];
        /// <summary>
        /// Gets or create extension configuration section
        /// </summary>
        public T GetOrCreateExtra<T>()
            where T : new()
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
        public void AddExtra<T>(T extra) => _extras[typeof(T)] = extra;

        public delegate bool XamlValueConverter(AstTransformationContext context,
            IXamlAstValueNode node, IXamlType type, out IXamlAstValueNode result);
        
        public IXamlTypeSystem TypeSystem { get; }
        public IXamlAssembly DefaultAssembly { get; }
        public XamlLanguageTypeMappings TypeMappings { get; }
        public XamlXmlnsMappings XmlnsMappings { get; }
        public XamlTypeWellKnownTypes WellKnownTypes { get; }
        public XamlValueConverter CustomValueConverter { get; }
        public List<(string ns, string name)> KnownDirectives { get; } = new List<(string, string)>
        {
            
        };

        public TransformerConfiguration(IXamlTypeSystem typeSystem, IXamlAssembly defaultAssembly,
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

            foreach (var contentAttributeOnType in GetCustomAttribute(type, TypeMappings.ContentAttributes))
            {
                if (contentAttributeOnType.Properties.Count == 0)
                {
                    throw new XamlTypeSystemException($"The '{contentAttributeOnType.Type}' attribute must have a property name specified");
                }
                if (contentAttributeOnType.Properties["Name"] is string propertyName)
                {
                    IXamlProperty contentProperty = type.GetAllProperties().FirstOrDefault(prop => prop.Name == propertyName);
                    if (contentProperty is null)
                    {
                        throw new XamlTypeSystemException($"The property name '{propertyName}' specified in the content property of {type.GetFqn()} does not exist.");
                    }

                    if (found != null && !contentProperty.Equals(found))
                        throw new XamlTypeSystemException(
                            "Content (or substitute) attribute is declared on multiple properties of " + type.GetFqn());
                    found = contentProperty;
                }
            }
            
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
        public IXamlType Void { get; }
        public IXamlType Boolean { get; }
        public IXamlType Double { get; }
        public IXamlType NullableT { get; }
        public IXamlType CultureInfo { get; }
        public IXamlType IFormatProvider { get; }
        public IXamlType Delegate { get; }

        public XamlTypeWellKnownTypes(IXamlTypeSystem typeSystem)
        {
            Void = typeSystem.GetType("System.Void");
            String = typeSystem.GetType("System.String");
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
        }
    }
}
