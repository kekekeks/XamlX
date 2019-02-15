using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using XamlX.Ast;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlXTransformerConfiguration
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

        public delegate bool XamlXValueConverter(XamlXAstTransformationContext context,
            IXamlXAstValueNode node, IXamlXType type, out IXamlXAstValueNode result);
        
        public IXamlXTypeSystem TypeSystem { get; }
        public IXamlXAssembly DefaultAssembly { get; }
        public XamlXLanguageTypeMappings TypeMappings { get; }
        public XamlXXmlnsMappings XmlnsMappings { get; }
        public XamlXTypeWellKnownTypes WellKnownTypes { get; }
        public XamlXValueConverter CustomValueConverter { get; }
        public List<(string ns, string name)> KnownDirectives { get; } = new List<(string, string)>
        {
            
        };

        public XamlXTransformerConfiguration(IXamlXTypeSystem typeSystem, IXamlXAssembly defaultAssembly,
            XamlXLanguageTypeMappings typeMappings, XamlXXmlnsMappings xmlnsMappings = null,
            XamlXValueConverter customValueConverter = null)
        {
            TypeSystem = typeSystem;
            DefaultAssembly = defaultAssembly;
            TypeMappings = typeMappings;
            XmlnsMappings = xmlnsMappings ?? XamlXXmlnsMappings.Resolve(typeSystem, typeMappings);
            WellKnownTypes = new XamlXTypeWellKnownTypes(typeSystem);
            CustomValueConverter = customValueConverter;
        }

        IDictionary<object, IXamlXProperty> _contentPropertyCache = new Dictionary<object, IXamlXProperty>();

        public IXamlXProperty FindContentProperty(IXamlXType type)
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
                        throw new XamlXTypeSystemException(
                            "Content (or substitute) attribute is declared on multiple properties of " + type.GetFqn());
                    found = p;
                }
            }

            return _contentPropertyCache[type.Id] = found;
        }
        

        public IEnumerable<IXamlXCustomAttribute> GetCustomAttribute(IXamlXType type, IXamlXType attributeType)
        {
            var custom = TypeMappings.CustomAttributeResolver?.GetCustomAttribute(type, attributeType);
            if (custom != null)
                yield return custom;
            foreach(var attr in type.CustomAttributes)
                if (attr.Type == attributeType)
                    yield return attr;
        }
        
        public IEnumerable<IXamlXCustomAttribute> GetCustomAttribute(IXamlXType type, IEnumerable<IXamlXType> types)
        {
            foreach(var t in types)
            foreach (var a in GetCustomAttribute(type, t))
                yield return a;
        }
        
        
        public IEnumerable<IXamlXCustomAttribute> GetCustomAttribute(IXamlXProperty prop, IXamlXType attributeType)
        {
            var custom = TypeMappings.CustomAttributeResolver?.GetCustomAttribute(prop, attributeType);
            if (custom != null)
                yield return custom;
            foreach(var attr in prop.CustomAttributes)
                if (attr.Type.Equals(attributeType))
                    yield return attr;
        }
        
        public IEnumerable<IXamlXCustomAttribute> GetCustomAttribute(IXamlXProperty prop, IEnumerable<IXamlXType> types)
        {
            foreach(var t in types)
            foreach (var a in GetCustomAttribute(prop, t))
                yield return a;
        }
    }

    public class XamlXTypeWellKnownTypes
    {
        public IXamlXType IList { get; }
        public IXamlXType IEnumerable { get; }
        public IXamlXType IEnumerableT { get; }
        public IXamlXType IListOfT { get; }
        public IXamlXType Object { get; }
        public IXamlXType String { get; }
        public IXamlXType Void { get; }
        public IXamlXType Boolean { get; }
        public IXamlXType Double { get; }
        public IXamlXType NullableT { get; }
        public IXamlXType CultureInfo { get; }
        public IXamlXType IFormatProvider { get; }
        public IXamlXType Delegate { get; }

        public XamlXTypeWellKnownTypes(IXamlXTypeSystem typeSystem)
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
