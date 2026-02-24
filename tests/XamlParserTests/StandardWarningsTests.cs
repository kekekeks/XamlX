using System;
using System.Diagnostics.CodeAnalysis;
using XamlX;
using Xunit;

namespace XamlParserTests;

[Obsolete("ObsoleteClass is obsolete")]
public class ObsoleteClass
{
    [Obsolete("ObjectProperty is obsolete")]
    public object? ObjectProperty { get; set; }

    [Obsolete("StaticProp is obsolete", true)]
    public static object StaticProp { get; } = "StaticPropValue";
}

#if NET8_0_OR_GREATER && !USE_NETSTANDARD_BUILD

[Experimental("FOO123")]
public class ExperimentalClass
{
    [Experimental("FOO123")]
    public object? ObjectProperty { get; set; }

    [Experimental("FOO123")]
    public static object StaticProp { get; } = "StaticPropValue";
}

#endif

#if NET10_0_OR_GREATER && !USE_NETSTANDARD_BUILD

[Experimental("FOO123", Message = "ExperimentalClass is experimental")]
public class ExperimentalWithMessageClass
{
    [Experimental("FOO123", Message = "ObjectProperty is experimental")]
    public object? ObjectProperty { get; set; }

    [Experimental("FOO123", Message = "StaticProp is experimental")]
    public static object StaticProp { get; } = "StaticPropValue";
}

#endif

public class StandardWarningsTests : CompilerTestBase
{
    private static void VerifyDiagnostic(XamlDiagnostic[] diagnostics, string title, string code, XamlDiagnosticSeverity severity)
        => Assert.Contains(diagnostics, d => d.Title == title && d.Code == code && d.Severity == severity);

    [Fact]
    public void Obsolete_Is_Reported()
    {
        Transform(@"
<ObsoleteClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
>
    <ObsoleteClass.ObjectProperty><x:Static Member='ObsoleteClass.StaticProp'/></ObsoleteClass.ObjectProperty>
</ObsoleteClass>");

        var diagnostics = Diagnostics.ToArray();

        VerifyDiagnostic(
            diagnostics,
            "'ObsoleteClass' is obsolete: ObsoleteClass is obsolete",
            "Obsolete",
            XamlDiagnosticSeverity.Warning);
        VerifyDiagnostic(
            diagnostics,
            "'ObsoleteClass.ObjectProperty' is obsolete: ObjectProperty is obsolete",
            "Obsolete",
            XamlDiagnosticSeverity.Warning);
        VerifyDiagnostic(
            diagnostics,
            "'ObsoleteClass.StaticProp' is obsolete: StaticProp is obsolete",
            "Obsolete",
            XamlDiagnosticSeverity.Error);
    }

#if NET8_0_OR_GREATER && !USE_NETSTANDARD_BUILD

    [Fact]
    public void Experimental_Is_Reported()
    {
        Transform(@"
<ExperimentalClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
>
    <ExperimentalClass.ObjectProperty><x:Static Member='ExperimentalClass.StaticProp'/></ExperimentalClass.ObjectProperty>
</ExperimentalClass>");

        var diagnostics = Diagnostics.ToArray();

        VerifyDiagnostic(
            diagnostics,
            "'ExperimentalClass' is for evaluation purposes only and is subject to change or removal in future updates.",
            "FOO123",
            XamlDiagnosticSeverity.Warning);
        VerifyDiagnostic(
            diagnostics,
            "'ExperimentalClass.ObjectProperty' is for evaluation purposes only and is subject to change or removal in future updates.",
            "FOO123",
            XamlDiagnosticSeverity.Warning);
        VerifyDiagnostic(
            diagnostics,
            "'ExperimentalClass.StaticProp' is for evaluation purposes only and is subject to change or removal in future updates.",
            "FOO123",
            XamlDiagnosticSeverity.Warning);
    }

#endif

#if NET10_0_OR_GREATER && !USE_NETSTANDARD_BUILD

    [Fact]
    public void Experimental_With_Message_Is_Reported()
    {
        Transform(@"
<ExperimentalWithMessageClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
>
    <ExperimentalWithMessageClass.ObjectProperty><x:Static Member='ExperimentalWithMessageClass.StaticProp'/></ExperimentalWithMessageClass.ObjectProperty>
</ExperimentalWithMessageClass>");

        var diagnostics = Diagnostics.ToArray();

        VerifyDiagnostic(
            diagnostics,
            "'ExperimentalWithMessageClass' is for evaluation purposes only and is subject to change or removal in future updates: 'ExperimentalClass is experimental'.",
            "FOO123",
            XamlDiagnosticSeverity.Warning);
        VerifyDiagnostic(
            diagnostics,
            "'ExperimentalWithMessageClass.ObjectProperty' is for evaluation purposes only and is subject to change or removal in future updates: 'ObjectProperty is experimental'.",
            "FOO123",
            XamlDiagnosticSeverity.Warning);
        VerifyDiagnostic(
            diagnostics,
            "'ExperimentalWithMessageClass.StaticProp' is for evaluation purposes only and is subject to change or removal in future updates: 'StaticProp is experimental'.",
            "FOO123",
            XamlDiagnosticSeverity.Warning);
    }

#endif

}
