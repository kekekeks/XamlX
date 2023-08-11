using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlILLocal : IXamlLocal
    {
        int Index { get; }
    }
}
