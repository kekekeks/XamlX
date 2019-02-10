using System;
using System.Collections.Generic;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlLanguageTypeMappings
    {
        public XamlIlLanguageTypeMappings(IXamlIlTypeSystem typeSystem)
        {
            ServiceProvider = typeSystem.GetType("System.IServiceProvider");
            TypeDescriptorContext = typeSystem.GetType("System.ComponentModel.ITypeDescriptorContext");
            SupportInitialize = typeSystem.GetType("System.ComponentModel.ISupportInitialize");
            var tconv = typeSystem.GetType("System.ComponentModel.TypeConverterAttribute");
            if (tconv != null)
                TypeConverterAttributes.Add(tconv);
        }

        public List<IXamlIlType> XmlnsAttributes { get; set; } = new List<IXamlIlType>();
        public List<IXamlIlType> UsableDuringInitializationAttributes { get; set; } = new List<IXamlIlType>();
        public List<IXamlIlType> ContentAttributes { get; set; } = new List<IXamlIlType>();
        public List<IXamlIlType> TypeConverterAttributes { get; set; } = new List<IXamlIlType>();
        public IXamlIlType ServiceProvider { get; set; }
        public IXamlIlType TypeDescriptorContext { get; set; }
        public IXamlIlType SupportInitialize { get; set; }
        public IXamlIlType ProvideValueTarget { get; set; }
        public IXamlIlType RootObjectProvider { get; set; }
        public IXamlIlType ParentStackProvider { get; set; }
        public IXamlIlType XmlNamespaceInfoProvider { get; set; }
        public IXamlIlType UriContextProvider { get; set; }
        
        public IXamlIlCustomAttributeResolver CustomAttributeResolver { get; set; }
        /// <summary>
        /// Expected signature:
        /// static void ApplyNonMatchingMarkupExtension(object target, object property, IServiceProvider prov, object value)
        /// </summary>
        public IXamlIlMethod MarkupExtensionCustomResultHandler { get; set; }
        public List<IXamlIlType> MarkupExtensionCustomResultTypes { get; set; } = new List<IXamlIlType>();
        public Func<IXamlIlProperty, IXamlIlType, bool> ShouldIgnoreMarkupExtensionCustomResultForProperty { get; set; }
        
        /// <summary>
        /// Expected signature:
        /// static IServiceProvider InnerServiceProviderFactory(IServiceProvider self);
        /// </summary>
        public IXamlIlMethod InnerServiceProviderFactoryMethod { get; set; }
        /// <summary>
        /// static Func&lt;IServiceProvider, object&gt; DeferredTransformationFactory(Func&lt;IServiceProvider, object&gt; builder, IServiceProvider provider);
        /// </summary>
        public IXamlIlMethod DeferredContentExecutorCustomization { get; set; }
        public List<IXamlIlType> DeferredContentPropertyAttributes { get; set; } = new List<IXamlIlType>();
    }

    public interface IXamlIlCustomAttributeResolver
    {
        IXamlIlCustomAttribute GetCustomAttribute(IXamlIlType type, IXamlIlType attributeType);
        IXamlIlCustomAttribute GetCustomAttribute(IXamlIlProperty property, IXamlIlType attributeType);
    }
}