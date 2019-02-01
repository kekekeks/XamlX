using System.Collections.Generic;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlXLanguageTypeMappings
    {
        public List<IXamlXType> XmlnsAttributes { get; set; } = new List<IXamlXType>();
        public List<IXamlXType> ContentAttributes { get; set; } = new List<IXamlXType>();
        public IXamlXCustomAttributeResolver CustomAttributeResolver { get; set; }        
    }

    public interface IXamlXCustomAttributeResolver
    {
        IXamlXCustomAttribute GetCustomAttribute(IXamlXType type, IXamlXType attributeType);
        IXamlXCustomAttribute GetCustomAttribute(IXamlXProperty property, IXamlXType attributeType);
    }
}