namespace XamlX;

#if !XAMLX_INTERNAL
public
#endif
enum XamlXDiagnosticCode
{
    Unknown = 0,

    ParseError,
    TransformError,
    EmitError,
    TypeSystemError,
    Obsolete
}