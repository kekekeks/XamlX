namespace XamlX;

internal static class TrimmingMessages
{
    public const string TrimmedAttributes =
        "Unreferenced attributes will be removed by the trimmer. We don't deal with it.";

    public const string CanBeSafelyTrimmed =
        "When this method is called, we don't care if any exported type was trimmed. We can't do anything here.";
    
    public const string TypePreservedElsewhere =
        "We assume that all IXamlType instances did preserve type information.";
    
    public const string GeneratedTypes =
        "This code references generated types that cannot be trimmed.";

    public const string Cecil = "Cecil is not getting trimmed.";
    
    public const string DynamicXamlReference =
        "x:Class directive type and XAML dependencies are referenced dynamically and might be trimmed.";
}
