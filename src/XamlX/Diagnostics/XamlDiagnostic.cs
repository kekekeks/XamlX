using System;

using XamlX.Ast;

namespace XamlX;

#nullable enable

#if !XAMLX_INTERNAL
public
#endif
    record XamlDiagnostic(string Code, XamlDiagnosticSeverity Severity, string Title, int? LineNumber, int? LinePosition) : IXamlLineInfo
{
    public XamlDiagnostic(string code, XamlDiagnosticSeverity severity, string title, IXamlLineInfo lineInfo)
        : this(code, severity, title, lineInfo.Line, lineInfo.Position)
    {
    }
    
    public string? Description { get; init; }

    public string? Document { get; init; }

    public Exception? InnerException { get; init; }

    public XamlXDiagnosticCode? XamlXCode { get; init; }

    int IXamlLineInfo.Line { get => LineNumber ?? 0; set { } }
    int IXamlLineInfo.Position { get => LinePosition ?? 0; set { } }
}