using System.Collections.Generic;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlLanguageTypeMappings
    {
        public List<IXamlIlType> XmlnsAttributes { get; set; } = new List<IXamlIlType>();
        public List<IXamlIlType> ContentAttributes { get; set; } = new List<IXamlIlType>();
        public IXamlIlCustomAttributeResolver CustomAttributeResolver { get; set; }        
    }

    public interface IXamlIlCustomAttributeResolver
    {
        IXamlIlCustomAttribute GetCustomAttribute(IXamlIlType type, IXamlIlType attributeType);
        IXamlIlCustomAttribute GetCustomAttribute(IXamlIlProperty property, IXamlIlType attributeType);
    }
}