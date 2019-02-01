using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlTransformerConfiguration
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
        
        public IXamlIlTypeSystem TypeSystem { get; }
        public IXamlIlAssembly DefaultAssembly { get; }
        public XamlIlLanguageTypeMappings TypeMappings { get; }
        public XamlIlXmlnsMappings XmlnsMappings { get; }
        public XamlIlTypeWellKnownTypes WellKnownTypes { get; }
        public List<(string ns, string name)> KnownDirectives { get; } = new List<(string, string)>
        {
            (XamlNamespaces.Xaml2006, "Arguments")
        };

        public XamlIlTransformerConfiguration(IXamlIlTypeSystem typeSystem, IXamlIlAssembly defaultAssembly,
            XamlIlLanguageTypeMappings typeMappings, XamlIlXmlnsMappings xmlnsMappings = null)
        {
            TypeSystem = typeSystem;
            DefaultAssembly = defaultAssembly;
            TypeMappings = typeMappings;
            XmlnsMappings = xmlnsMappings ?? XamlIlXmlnsMappings.Resolve(typeSystem, typeMappings);
            WellKnownTypes = new XamlIlTypeWellKnownTypes(typeSystem);
        }

        IDictionary<object, IXamlIlProperty> _contentPropertyCache = new Dictionary<object, IXamlIlProperty>();

        public IXamlIlProperty FindContentProperty(IXamlIlType type)
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
                        throw new XamlIlTypeSystemException(
                            "Content (or substitute) attribute is declared on multiple properties of " + type.GetFqn());
                    found = p;
                }
            }

            return _contentPropertyCache[type.Id] = found;
        }
        

        public IEnumerable<IXamlIlCustomAttribute> GetCustomAttribute(IXamlIlType type, IXamlIlType attributeType)
        {
            var custom = TypeMappings.CustomAttributeResolver?.GetCustomAttribute(type, attributeType);
            if (custom != null)
                yield return custom;
            foreach(var attr in type.CustomAttributes)
                if (attr.Type == attributeType)
                    yield return attr;
        }
        
        public IEnumerable<IXamlIlCustomAttribute> GetCustomAttribute(IXamlIlType type, IEnumerable<IXamlIlType> types)
        {
            foreach(var t in types)
            foreach (var a in GetCustomAttribute(type, t))
                yield return a;
        }
        
        
        public IEnumerable<IXamlIlCustomAttribute> GetCustomAttribute(IXamlIlProperty prop, IXamlIlType attributeType)
        {
            var custom = TypeMappings.CustomAttributeResolver?.GetCustomAttribute(prop, attributeType);
            if (custom != null)
                yield return custom;
            foreach(var attr in prop.CustomAttributes)
                if (attr.Type == attributeType)
                    yield return attr;
        }
        
        public IEnumerable<IXamlIlCustomAttribute> GetCustomAttribute(IXamlIlProperty prop, IEnumerable<IXamlIlType> types)
        {
            foreach(var t in types)
            foreach (var a in GetCustomAttribute(prop, t))
                yield return a;
        }

        public bool TryCallAdd(IXamlIlType type, IXamlIlAstValueNode value, out XamlIlInstanceMethodCallNode rv)
        {
            rv = null;
            var method = type.FindMethod("Add", WellKnownTypes.Void, true, value.Type.GetClrType());
            if (method != null)
            {
                rv = new XamlIlInstanceMethodCallNode(value, method, new[] {value});
                return true;
            }
            return false;
        }
        
        public bool TryGetCorrectlyTypedValue(IXamlIlAstValueNode node, IXamlIlType type, out IXamlIlAstValueNode rv)
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

    public class XamlIlTypeWellKnownTypes
    {
        public IXamlIlType IList { get; }
        public IXamlIlType IListOfT { get; }
        public IXamlIlType Object { get;  }
        public IXamlIlType String { get;  }
        public IXamlIlType Void { get;  }

        public XamlIlTypeWellKnownTypes(IXamlIlTypeSystem typeSystem)
        {
            Void = typeSystem.FindType("System.Void");
            String = typeSystem.FindType("System.String");
            Object = typeSystem.FindType("System.Object");
            IList = typeSystem.FindType("System.Collections.IList");
            IListOfT = typeSystem.FindType("System.Collections.Generic.IList`1");
        }
    }
}