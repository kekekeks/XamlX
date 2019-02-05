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
        
        public IXamlTypeSystem TypeSystem { get; }
        public IXamlAssembly DefaultAssembly { get; }
        public XamlLanguageTypeMappings TypeMappings { get; }
        public XamlXmlnsMappings XmlnsMappings { get; }
        public XamlTypeWellKnownTypes WellKnownTypes { get; }
        public List<(string ns, string name)> KnownDirectives { get; } = new List<(string, string)>
        {
            (XamlNamespaces.Xaml2006, "Arguments")
        };

        public XamlTransformerConfiguration(IXamlTypeSystem typeSystem, IXamlAssembly defaultAssembly,
            XamlLanguageTypeMappings typeMappings, XamlXmlnsMappings xmlnsMappings = null)
        {
            TypeSystem = typeSystem;
            DefaultAssembly = defaultAssembly;
            TypeMappings = typeMappings;
            XmlnsMappings = xmlnsMappings ?? XamlXmlnsMappings.Resolve(typeSystem, typeMappings);
            WellKnownTypes = new XamlTypeWellKnownTypes(typeSystem);
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
                    if (found != null)
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
                if (attr.Type == attributeType)
                    yield return attr;
        }
        
        public IEnumerable<IXamlCustomAttribute> GetCustomAttribute(IXamlProperty prop, IEnumerable<IXamlType> types)
        {
            foreach(var t in types)
            foreach (var a in GetCustomAttribute(prop, t))
                yield return a;
        }

        public bool TryCallAdd(IXamlType type, IXamlAstValueNode value, out XamlXInstanceNoReturnMethodCallNode rv)
        {
            rv = null;
            var method = type.FindMethod("Add", WellKnownTypes.Void, true, value.Type.GetClrType());
            if (method != null)
            {
                rv = new XamlXInstanceNoReturnMethodCallNode(value, method, new[] {value});
                return true;
            }
            return false;
        }
        
        public bool TryGetCorrectlyTypedValue(IXamlAstValueNode node, IXamlType type, out IXamlAstValueNode rv)
        {
            rv = null;
            if (type.IsAssignableFrom(node.Type.GetClrType()))
            {
                rv = node;
                return true;
            }
            //TODO: Converters
            return false;
        }
    }

    public class XamlTypeWellKnownTypes
    {
        public IXamlType IList { get; }
        public IXamlType IListOfT { get; }
        public IXamlType Object { get;  }
        public IXamlType String { get;  }
        public IXamlType Void { get;  }

        public XamlTypeWellKnownTypes(IXamlTypeSystem typeSystem)
        {
            Void = typeSystem.FindType("System.Void");
            String = typeSystem.FindType("System.String");
            Object = typeSystem.FindType("System.Object");
            IList = typeSystem.FindType("System.Collections.IList");
            IListOfT = typeSystem.FindType("System.Collections.Generic.IList`1");
        }
    }
}