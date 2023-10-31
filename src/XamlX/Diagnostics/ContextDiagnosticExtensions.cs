using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using XamlX.Ast;
using XamlX.Transform;

#nullable enable

namespace XamlX;

#if NET6_0_OR_GREATER
[StackTraceHidden]
#endif
#if !XAMLX_INTERNAL
public
#endif
static class ContextDiagnosticExtensions
{
    public static IXamlAstNode ReportDiagnostic(
        this AstTransformationContext context,
        XamlXDiagnosticCode diagnosticCode,
        XamlDiagnosticSeverity minSeverity,
        string title,
        IXamlAstNode offender,
        string? description = null)
    {
        var code = context.Configuration.DiagnosticsHandler.CodeMappings(diagnosticCode);
        var diagnostic = new XamlDiagnostic(code, minSeverity, title, offender?.Line, offender?.Position)
        {
            Description = description,
            XamlXCode = diagnosticCode
        };
        context.ReportDiagnostic(diagnostic);
        return offender;
    }

    public static IXamlAstNode ReportTransformError(this AstTransformationContext context, string title, IXamlAstNode offender) =>
        ReportTransformError(context, title, offender, offender);

    public static TReturn ReportTransformError<TReturn>(this AstTransformationContext context, string title, IXamlLineInfo? offender, TReturn ret) =>
        ReportError(context, XamlXDiagnosticCode.TransformError, title, offender, ret);

    public static IXamlAstNode ReportError(this AstTransformationContext context, XamlXDiagnosticCode diagnosticCode, string title, IXamlAstNode node) =>
        ReportError(context, diagnosticCode, title, node, node);

    public static TReturn ReportError<TReturn>(this AstTransformationContext context, XamlXDiagnosticCode diagnosticCode, string title, IXamlLineInfo? offender, TReturn ret)
    {
        var code = context.Configuration.DiagnosticsHandler.CodeMappings(diagnosticCode);
        var diagnostic = new XamlDiagnostic(code, XamlDiagnosticSeverity.Error, title, offender?.Line,
            offender?.Position)
        {
            XamlXCode = diagnosticCode
        };
        context.ReportDiagnostic(diagnostic, throwOnFatal: true);
        return ret;
    }

    public static XamlDiagnostic ToDiagnostic(this Exception exception, AstTransformationContext context, string? document = null)
    {
        var code = context.Configuration.DiagnosticsHandler.CodeMappings(ToDiagnosticId(exception));
        return exception.ToDiagnostic(code, document);
    }
    
    public static XamlDiagnostic ToDiagnostic(this Exception exception, string code, string? document = null)
    {
        var lineInfo = exception as XmlException;
        return new XamlDiagnostic(
            code, XamlDiagnosticSeverity.Error,
            exception.Message,
            lineInfo?.LineNumber, lineInfo?.LinePosition)
        {
            Description = exception.ToString(),
            Document = (exception as XamlParseException)?.Document ?? document,
            InnerException = exception
        };
    }
    
    public static XamlXDiagnosticCode ToDiagnosticId(this Exception exception)
    {
        return exception switch
        {
            XamlTransformException => XamlXDiagnosticCode.TransformError,
            XamlLoadException => XamlXDiagnosticCode.EmitError,
            XamlTypeSystemException => XamlXDiagnosticCode.TypeSystemError,
            _ => XamlXDiagnosticCode.ParseError
        };
    }

    public static Exception ToException(this XamlDiagnostic diagnostic)
    {
        if (diagnostic.InnerException is XmlException or XamlTypeSystemException)
            return diagnostic.InnerException;

        return diagnostic.XamlXCode switch
        {
            XamlXDiagnosticCode.TransformError =>
                new XamlTransformException(diagnostic.Title, diagnostic, diagnostic.InnerException) { Document = diagnostic.Document },
            XamlXDiagnosticCode.EmitError =>
                new XamlLoadException(diagnostic.Title, diagnostic, diagnostic.InnerException) { Document = diagnostic.Document },
            XamlXDiagnosticCode.TypeSystemError =>
                new XamlTypeSystemException(diagnostic.Title, diagnostic.InnerException),
            _ => new XamlParseException(diagnostic.Title, diagnostic, diagnostic.InnerException) { Document = diagnostic.Document },
        };
    }

    public static void ThrowExceptionIfAnyError(this IEnumerable<XamlDiagnostic> diagnostics)
    {
        var errors = diagnostics.Where(diagnostic => diagnostic.Severity >= XamlDiagnosticSeverity.Error)
            .Select(d => d.ToException())
            .ToArray();
        if (errors.Length == 1)
        {
            throw errors[0];
        }
        else if (errors.Length > 0)
        {
            throw new AggregateException(errors);
        }
    }
}