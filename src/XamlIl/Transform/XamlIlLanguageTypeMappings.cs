using System.Collections.Generic;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlLanguageTypeMappings
    {
        public XamlIlLanguageTypeMappings(IXamlIlTypeSystem typeSystem)
        {
            ServiceProvider = typeSystem.FindType("System.IServiceProvider");
        }

        public List<IXamlIlType> XmlnsAttributes { get; set; } = new List<IXamlIlType>();
        public List<IXamlIlType> ContentAttributes { get; set; } = new List<IXamlIlType>();
        public IXamlIlType ServiceProvider { get; set; }
        public IXamlIlType RootObjectProvider { get; set; }
        public IXamlIlCustomAttributeResolver CustomAttributeResolver { get; set; }
        /// <summary>
        /// Expected signature:
        /// static void ApplyNonMatchingMarkupExtension(object target, string property, IServiceProvider prov, object value)
        /// </summary>
        public IXamlIlMethod ApplyNonMatchingMarkupExtension { get; set; }
    }

    public interface IXamlIlCustomAttributeResolver
    {
        IXamlIlCustomAttribute GetCustomAttribute(IXamlIlType type, IXamlIlType attributeType);
        IXamlIlCustomAttribute GetCustomAttribute(IXamlIlProperty property, IXamlIlType attributeType);
    }
}