using System.IO;
using System.Linq;
using XamlX.TypeSystem;

namespace TypeSystemTest;

public class CecilTest : BaseTest
{
    public CecilTest() : base(CreateTypeSystem())
    {
    }

    public static IXamlTypeSystem CreateTypeSystem ()
    {
        var self = typeof(BaseTest).Assembly.GetModules()[0].FullyQualifiedName;
#if USE_NETSTANDARD_BUILD
            var selfDir = Path.GetDirectoryName(self)!;
            var selfName = Path.GetFileName(self);
            self = Path.GetFullPath(Path.Combine(selfDir, "../netstandard2.0/", selfName));
#endif
        var refsPath = self + ".refs";
        var refs = File.ReadAllLines(refsPath).Concat([self]);
        return new CecilTypeSystem(refs, null);
    }
}
