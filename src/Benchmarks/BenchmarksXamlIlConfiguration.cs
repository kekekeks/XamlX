using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Benchmarks
{
    class BenchmarksXamlIlConfiguration
    {
        public static XamlIlTransformerConfiguration Configure(IXamlIlTypeSystem typeSystem)
        {
            return new XamlIlTransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("Benchmarks"),
                new XamlIlLanguageTypeMappings(typeSystem)
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
                    ParentStackProvider = typeSystem.GetType("XamlIl.Runtime.IXamlIlParentStackProviderV1")
                });
        }
    }
}