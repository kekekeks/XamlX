using XamlX.Transform;
using XamlX.TypeSystem;

namespace Benchmarks
{
    class BenchmarksXamlXConfiguration
    {
        public static TransformerConfiguration Configure(IXamlTypeSystem typeSystem)
        {
            return new TransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("Benchmarks"),
                new XamlLanguageTypeMappings(typeSystem)
                {
                    XmlnsAttributes =
                    {
                        typeSystem.GetType("Portable.Xaml.Markup.XmlnsDefinitionAttribute"),
                    },
                    ContentAttributes =
                    {
                        typeSystem.GetType("Benchmarks.ContentAttribute")
                    },
                    RootObjectProvider = typeSystem.GetType("Portable.Xaml.IRootObjectProvider"),
                    ParentStackProvider = typeSystem.GetType("XamlX.Runtime.IXamlParentStackProviderV1")
                });
        }
    }
}