namespace System;

/// <summary>
/// Source https://stackoverflow.com/a/46382137/20894223
/// </summary>
internal static class TypeExtensions
{
    public static bool IsInternal(this Type t) => !t.IsVisible
            && !t.IsPublic
            && t.IsNotPublic
            && !t.IsNested
            && !t.IsNestedPublic
            && !t.IsNestedFamily
            && !t.IsNestedPrivate
            && !t.IsNestedAssembly
            && !t.IsNestedFamORAssem
            && !t.IsNestedFamANDAssem;

    // only nested types can be declared "protected"
    public static bool IsProtected(this Type t) => !t.IsVisible
            && !t.IsPublic
            && !t.IsNotPublic
            && t.IsNested
            && !t.IsNestedPublic
            && t.IsNestedFamily
            && !t.IsNestedPrivate
            && !t.IsNestedAssembly
            && !t.IsNestedFamORAssem
            && !t.IsNestedFamANDAssem;

    // only nested types can be declared "private"
    public static bool IsPrivate(Type t)
    {
        return
            !t.IsVisible
            && !t.IsPublic
            && !t.IsNotPublic
            && t.IsNested
            && !t.IsNestedPublic
            && !t.IsNestedFamily
            && t.IsNestedPrivate
            && !t.IsNestedAssembly
            && !t.IsNestedFamORAssem
            && !t.IsNestedFamANDAssem;
    }
}
