using System.Collections.Generic;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlXLanguageTypeMappings
    {
        public XamlXLanguageTypeMappings(IXamlXTypeSystem typeSystem)
        {
            ServiceProvider = typeSystem.FindType("System.IServiceProvider");
            TypeDescriptorContext = typeSystem.FindType("System.ComponentModel.ITypeDescriptorContext");
            SupportInitialize = typeSystem.FindType("System.ComponentModel.ISupportInitialize");
        }

        public List<IXamlXType> XmlnsAttributes { get; set; } = new List<IXamlXType>();
        public List<IXamlXType> UsableDuringInitializationAttributes { get; set; } = new List<IXamlXType>();
        public List<IXamlXType> ContentAttributes { get; set; } = new List<IXamlXType>();
        public IXamlXType ServiceProvider { get; set; }
        public IXamlXType TypeDescriptorContext { get; set; }
        public IXamlXType SupportInitialize { get; set; }
        public IXamlXType RootObjectProvider { get; set; }
        public IXamlXType ParentStackProvider { get; set; }
        public IXamlXCustomAttributeResolver CustomAttributeResolver { get; set; }
        /// <summary>
        /// Expected signature:
        /// static void ApplyNonMatchingMarkupExtension(object target, string property, IServiceProvider prov, object value)
        /// </summary>
        public IXamlXMethod ApplyNonMatchingMarkupExtension { get; set; }
    }

    public interface IXamlXCustomAttributeResolver
    {
        IXamlXCustomAttribute GetCustomAttribute(IXamlXType type, IXamlXType attributeType);
        IXamlXCustomAttribute GetCustomAttribute(IXamlXProperty property, IXamlXType attributeType);
    }
}