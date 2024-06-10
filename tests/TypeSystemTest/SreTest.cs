using System.Reflection;
using XamlX.IL;
using XamlX.TypeSystem;

namespace TypeSystemTest;

public class SreTest : BaseTest
{
    public SreTest() : base(CreateTypeSystem())
    {

    }
    public static IXamlTypeSystem CreateTypeSystem()
    {
        Assembly.Load("XamlX.Runtime");
        return new SreTypeSystem();
    }
}
