namespace System;

/// <summary>
/// Source https://stackoverflow.com/a/46382137/20894223
/// </summary>
internal static class TypeExtensions
{
    public static bool IsTopLevelInternal(this Type t) => !t.IsVisible
            && !t.IsPublic
            && t.IsNotPublic
            && !t.IsNested
            && !t.IsNestedPublic
            && !t.IsNestedFamily
            && !t.IsNestedPrivate
            && !t.IsNestedAssembly
            && !t.IsNestedFamORAssem
            && !t.IsNestedFamANDAssem;

    public static bool IsNestedlPublic_Or_Internal(this Type t) => !t.IsPublic
        && t.IsNested
        && (t.IsNestedPublic || t.IsNestedAssembly);
}
