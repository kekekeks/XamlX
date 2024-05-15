using System;

namespace XamlX.Transform;

#if !XAMLX_INTERNAL
    public
#endif
class XamlDiagnosticsHandler
{
    public Func<object, string> CodeMappings { get; init; } = code => code.ToString() ?? string.Empty;

    public Func<Exception, string> ExceptionFormatter { get; init; } = ex => ex.Message;

    public Func<XamlDiagnostic, XamlDiagnosticSeverity>? HandleDiagnostic { get; init; }

    internal XamlDiagnosticSeverity ReportDiagnostic(XamlDiagnostic diagnostic)
    {
        var severity = HandleDiagnostic?.Invoke(diagnostic) ?? diagnostic.Severity;
        return severity > diagnostic.MinSeverity ? severity : diagnostic.MinSeverity;
    }
}