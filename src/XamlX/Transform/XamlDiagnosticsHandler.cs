using System;

namespace XamlX.Transform;

#nullable enable

#if !XAMLX_INTERNAL
    public
#endif
class XamlDiagnosticsHandler
{
    public Func<XamlXDiagnosticCode, string> CodeMappings { get; init; } =
        code => FormattableString.Invariant($"XMLX{(int)code:0000}"); 

    public Func<XamlDiagnostic, XamlDiagnosticSeverity>? HandleDiagnostic { get; init; }

    internal XamlDiagnosticSeverity ReportDiagnostic(XamlDiagnostic diagnostic)
    {
        var severity = HandleDiagnostic?.Invoke(diagnostic) ?? diagnostic.Severity;
        return severity > diagnostic.MinSeverity ? severity : diagnostic.MinSeverity;
    }
}