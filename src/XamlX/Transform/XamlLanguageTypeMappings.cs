using System.Collections.Generic;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlLanguageTypeMappings
    {
        public XamlLanguageTypeMappings(IXamlTypeSystem typeSystem)
        {
            ServiceProvider = typeSystem.GetType("System.IServiceProvider");
            TypeDescriptorContext = typeSystem.GetType("System.ComponentModel.ITypeDescriptorContext");
            SupportInitialize = typeSystem.GetType("System.ComponentModel.ISupportInitialize");
            var tconv = typeSystem.GetType("System.ComponentModel.TypeConverterAttribute");
            if (tconv != null)
                TypeConverterAttributes.Add(tconv);
        }

        public XamlLanguageTypeMappings(IXamlTypeSystem typeSystem, bool useDefault)
        {
            if (useDefault)
            {
                ServiceProvider = typeSystem.GetType("System.IServiceProvider");
                TypeDescriptorContext = typeSystem.GetType("System.ComponentModel.ITypeDescriptorContext");
                SupportInitialize = typeSystem.GetType("System.ComponentModel.ISupportInitialize");
                var tconv = typeSystem.GetType("System.ComponentModel.TypeConverterAttribute");
                if (tconv != null)
                    TypeConverterAttributes.Add(tconv);
            }
        }

        public List<IXamlType> XmlnsAttributes { get; set; } = new List<IXamlType>();
        public List<IXamlType> UsableDuringInitializationAttributes { get; set; } = new List<IXamlType>();
        public List<IXamlType> ContentAttributes { get; set; } = new List<IXamlType>();
        public List<IXamlType> TypeConverterAttributes { get; set; } = new List<IXamlType>();
        public IXamlType ServiceProvider { get; set; }
        public IXamlType TypeDescriptorContext { get; set; }
        public IXamlType SupportInitialize { get; set; }
        public IXamlType ProvideValueTarget { get; set; }
        public IXamlType RootObjectProvider { get; set; }
        public IXamlType ParentStackProvider { get; set; }
        public IXamlType XmlNamespaceInfoProvider { get; set; }
        public IXamlType UriContextProvider { get; set; }
        
        public IXamlCustomAttributeResolver CustomAttributeResolver { get; set; }
        
        /// <summary>
        /// Expected signature:
        /// static IServiceProvider InnerServiceProviderFactory(IServiceProvider self);
        /// </summary>
        public IXamlMethod InnerServiceProviderFactoryMethod { get; set; }
        /// <summary>
        /// static Func&lt;IServiceProvider, object&gt; DeferredTransformationFactory(Func&lt;IServiceProvider, object&gt; builder, IServiceProvider provider);
        /// </summary>
        public IXamlMethod DeferredContentExecutorCustomization { get; set; }
        public List<IXamlType> DeferredContentPropertyAttributes { get; set; } = new List<IXamlType>();
        public string RootObjectProviderIntermediateRootPropertyName { get; set; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlCustomAttributeResolver
    {
        IXamlCustomAttribute GetCustomAttribute(IXamlType type, IXamlType attributeType);
        IXamlCustomAttribute GetCustomAttribute(IXamlProperty property, IXamlType attributeType);
    }
}
