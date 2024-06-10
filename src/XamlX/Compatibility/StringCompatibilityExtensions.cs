#if !NET6_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace System;

public static class StringCompatibilityExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string str, char search) =>
        str.Contains(search.ToString());
}
#endif