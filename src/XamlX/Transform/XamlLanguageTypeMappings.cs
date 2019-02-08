using System.Collections.Generic;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlLanguageTypeMappings
    {
        public XamlLanguageTypeMappings(IXamlTypeSystem typeSystem)
        {
            ServiceProvider = typeSystem.FindType("System.IServiceProvider");
            TypeDescriptorContext = typeSystem.FindType("System.ComponentModel.ITypeDescriptorContext");
            SupportInitialize = typeSystem.FindType("System.ComponentModel.ISupportInitialize");
        }

        public List<IXamlType> XmlnsAttributes { get; set; } = new List<IXamlType>();
        public List<IXamlType> UsableDuringInitializationAttributes { get; set; } = new List<IXamlType>();
        public List<IXamlType> ContentAttributes { get; set; } = new List<IXamlType>();
        public IXamlType ServiceProvider { get; set; }
        public IXamlType TypeDescriptorContext { get; set; }
        public IXamlType SupportInitialize { get; set; }
        public IXamlType RootObjectProvider { get; set; }
        public IXamlType ParentStackProvider { get; set; }
        public IXamlCustomAttributeResolver CustomAttributeResolver { get; set; }
        /// <summary>
        /// Expected signature:
        /// static void ApplyNonMatchingMarkupExtension(object target, string property, IServiceProvider prov, object value)
        /// </summary>
        public IXamlMethod ApplyNonMatchingMarkupExtension { get; set; }
    }

    public interface IXamlCustomAttributeResolver
    {
        IXamlCustomAttribute GetCustomAttribute(IXamlType type, IXamlType attributeType);
        IXamlCustomAttribute GetCustomAttribute(IXamlProperty property, IXamlType attributeType);
    }
}