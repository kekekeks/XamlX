using XamlX.Transform;
using XamlX.TypeSystem;

namespace Benchmarks
{
    class BenchmarksXamlXConfiguration
    {
        public static XamlXTransformerConfiguration Configure(IXamlXTypeSystem typeSystem)
        {
            return new XamlXTransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("Benchmarks"),
                new XamlXLanguageTypeMappings(typeSystem)
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
                    ParentStackProvider = typeSystem.GetType("XamlX.Runtime.IXamlXParentStackProviderV1")
                });
        }
    }
}