using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#if !NET6_0_OR_GREATER && !XAMLX_INTERNAL
namespace System.Runtime.CompilerServices
{
    /// <summary>
    ///     Reserved to be used by the compiler for tracking metadata.
    ///     This class should not be used by developers in source code.
    /// </summary>
    [ExcludeFromCodeCoverage, DebuggerNonUserCode]
    internal static class IsExternalInit
    {
    }
}
#endif