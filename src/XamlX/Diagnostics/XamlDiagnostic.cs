using System;

using XamlX.Ast;

namespace XamlX;

#nullable enable

#if !XAMLX_INTERNAL
public
#endif
    record XamlDiagnostic(
        string Code,
        XamlDiagnosticSeverity Severity,
        string Title,
        int? LineNumber,
        int? LinePosition,
        int? SpanStart,
        int? SpanEnd) : IXamlLineInfo
{
    public XamlDiagnostic(string code, XamlDiagnosticSeverity severity, string title, IXamlLineInfo? lineInfo = null)
        : this(code, severity, title, lineInfo?.Line, lineInfo?.Position, lineInfo?.SpanStart, lineInfo?.SpanEnd)
    {
    }

    public XamlDiagnosticSeverity MinSeverity { get; init; }

    public string? Document { get; init; }

    internal Exception? InnerException { get; init; }

    int IXamlLineInfo.Line { get => LineNumber ?? 0; set { } }
    int IXamlLineInfo.Position { get => LinePosition ?? 0; set { } }
    int IXamlLineInfo.SpanStart => SpanStart ?? -1;
    int IXamlLineInfo.SpanEnd => SpanEnd ?? -1;
    object? IXamlLineInfo.XmlNode => null;
}