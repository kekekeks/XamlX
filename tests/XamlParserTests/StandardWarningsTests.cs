using System;
using XamlX;
using Xunit;

namespace XamlParserTests;

[Obsolete("StandardWarningsTestsClass is obsolete")]
public class StandardWarningsTestsClass
{
    [Obsolete("ObjectProperty is obsolete")]
    public object? ObjectProperty { get; set; }

    [Obsolete("StaticProp is obsolete", true)]
    public static object StaticProp { get; } = "StaticPropValue";
}

public class StandardWarningsTests : CompilerTestBase
{
    [Fact]
    public void Static_Extension_Resolves_Values()
    {
        Transform($@"
<StandardWarningsTestsClass 
    xmlns='test' 
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
>
    <StandardWarningsTestsClass.ObjectProperty><x:Static Member='StandardWarningsTestsClass.StaticProp'/></StandardWarningsTestsClass.ObjectProperty>
</StandardWarningsTestsClass>");

        var obsoletes = Diagnostics.ToArray();
        Assert.Contains(obsoletes, d => d.Title.Contains("StandardWarningsTestsClass is obsolete") && d is { Code: "Obsolete", Severity: XamlDiagnosticSeverity.Warning });
        Assert.Contains(obsoletes, d => d.Title.Contains("ObjectProperty is obsolete") && d is { Code: "Obsolete", Severity: XamlDiagnosticSeverity.Warning });
        Assert.Contains(obsoletes, d => d.Title.Contains("StaticProp is obsolete") && d is { Code: "Obsolete", Severity: XamlDiagnosticSeverity.Error });
    }
}