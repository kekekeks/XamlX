using System.Runtime.CompilerServices;

#if DEBUG && !XAMLX_INTERNAL
[assembly: InternalsVisibleTo("XamlParserTests")]
#endif