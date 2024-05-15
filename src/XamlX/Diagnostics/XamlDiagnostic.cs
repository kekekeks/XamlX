using System;

using XamlX.Ast;

namespace XamlX;

#if !XAMLX_INTERNAL
public
#endif
    record XamlDiagnostic(
        string Code,
        XamlDiagnosticSeverity Severity,
        string Title,
        int? LineNumber, int? LinePosition) : IXamlLineInfo
{
    public XamlDiagnostic(string code, XamlDiagnosticSeverity severity, string title, IXamlLineInfo? lineInfo = null)
        : this(code, severity, title, lineInfo?.Line, lineInfo?.Position)
    {
    }

    public XamlDiagnosticSeverity MinSeverity { get; init; }

    public string? Document { get; init; }

    internal Exception? InnerException { get; init; }

    int IXamlLineInfo.Line { get => LineNumber ?? 0; set { } }
    int IXamlLineInfo.Position { get => LinePosition ?? 0; set { } }
}