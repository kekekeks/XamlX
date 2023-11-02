﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
#if NET6_0_OR_GREATER
    [return: NotNullIfNotNull(nameof(offender))]
#endif
    public static IXamlAstNode? ReportDiagnostic(
        this AstTransformationContext context,
        string diagnosticCode,
        XamlDiagnosticSeverity severity,
        string title,
        IXamlAstNode? offender,
        string? description = null,
        XamlDiagnosticSeverity minSeverity = XamlDiagnosticSeverity.None)
    {
        var diagnostic = new XamlDiagnostic(diagnosticCode, severity, title, offender?.Line, offender?.Position)
        {
            Description = description,
            Document = context.Document,
            MinSeverity = minSeverity
        };
        context.ReportDiagnostic(diagnostic);
        return offender;
    }

    public static IXamlAstNode ReportTransformError(this AstTransformationContext context, string title, IXamlAstNode offender) =>
        ReportTransformError(context, title, offender, offender);

    public static TReturn ReportTransformError<TReturn>(this AstTransformationContext context, string title, IXamlLineInfo? offender, TReturn ret) =>
        ReportError(context, new XamlTransformException(title, offender), ret);

    public static TReturn ReportError<TReturn>(this AstTransformationContext context, Exception exception, TReturn ret)
    {
        var diagnostic = exception.ToDiagnostic(context);
        context.ReportDiagnostic(diagnostic, throwOnFatal: true);
        return ret;
    }

    public static XamlDiagnostic ToDiagnostic(this Exception exception, AstTransformationContext context, string? document = null)
    {
        var code = (exception as XamlParseException)?.DiagnosticCode ??
                   context.Configuration.DiagnosticsHandler.CodeMappings(exception);
        return exception.ToDiagnostic(code, document);
    }
    
    private static XamlDiagnostic ToDiagnostic(this Exception exception, string code, string? document = null)
    {
        var lineInfo = exception as XmlException;
        return new XamlDiagnostic(
            code, XamlDiagnosticSeverity.Error,
            exception.Message,
            lineInfo?.LineNumber, lineInfo?.LinePosition)
        {
            Description = exception.ToString(),
            Document = (exception as XamlParseException)?.Document ?? document,
            MinSeverity = XamlDiagnosticSeverity.Error,
            InnerException = exception
        };
    }

    public static Exception ToException(this XamlDiagnostic diagnostic)
    {
        if (diagnostic.InnerException is XmlException or XamlTypeSystemException)
            return diagnostic.InnerException;

        return new XamlParseException(diagnostic.Title, diagnostic, diagnostic.Code, diagnostic.InnerException) { Document = diagnostic.Document };
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