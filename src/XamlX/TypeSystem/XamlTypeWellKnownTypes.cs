using System;
using System.Diagnostics.CodeAnalysis;

namespace XamlX.TypeSystem;

#if !XAMLX_INTERNAL
public
#endif
class XamlTypeWellKnownTypes
{
    private readonly IXamlType[] _actionOfT;
    private readonly IXamlType[] _funcOfT;

    public IXamlType Action { get; }
    public IXamlType Array { get; }
    public IXamlType Boolean { get; }
    public IXamlType CultureInfo { get; }
    public IXamlType Delegate { get; }
    public IXamlType DictionaryOfT2 { get; }
    public IXamlType Double { get; }
    public IXamlType IDisposable { get; }
    public IXamlType IEnumerable { get; }
    public IXamlType IEnumerableOfT { get; }
    public IXamlType IEnumerator { get; }
    public IXamlType IEnumeratorOfT { get; }
    public IXamlType IFormatProvider { get; }
    public IXamlType IList { get; }
    public IXamlType IListOfT { get; }
    public IXamlType IReadOnlyListOfT { get; }
    public IXamlType Int32 { get; }
    public IXamlType IntPtr { get; }
    public IXamlType InvalidCastException { get; }
    public IXamlType ListOfT { get; }
    public IXamlType MethodInfo { get; }
    public IXamlType MulticastDelegate { get; }
    public IXamlType NotSupportedException { get; }
    public IXamlType NullReferenceException { get; }
    public IXamlType NullableT { get; }
    public IXamlType Object { get; }
    public IXamlType ObsoleteAttribute { get; }
    public IXamlType String { get; }
    public IXamlType Type { get; }
    public IXamlType Uri { get; }
    public IXamlType Void { get; }
    public IXamlType? ExperimentalAttribute { get; }

    public IXamlType GetActionOfT(int typeParamCount)
        => _actionOfT[typeParamCount - 1];

    public IXamlType GetFuncOfT(int typeParamCount)
        => _funcOfT[typeParamCount - 1];

    [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = TrimmingMessages.TypeInCoreAssembly)]
    [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypeInCoreAssembly)]
    public XamlTypeWellKnownTypes(IXamlTypeSystem typeSystem)
    {
        Action = typeSystem.GetType("System.Action");
        Array = typeSystem.GetType("System.Array");
        Boolean = typeSystem.GetType("System.Boolean");
        CultureInfo = typeSystem.GetType("System.Globalization.CultureInfo");
        Delegate = typeSystem.GetType("System.Delegate");
        DictionaryOfT2 = typeSystem.GetType("System.Collections.Generic.Dictionary`2");
        Double = typeSystem.GetType("System.Double");
        IDisposable = typeSystem.GetType("System.IDisposable");
        IEnumerable = typeSystem.GetType("System.Collections.IEnumerable");
        IEnumerableOfT = typeSystem.GetType("System.Collections.Generic.IEnumerable`1");
        IEnumerator = typeSystem.GetType("System.Collections.IEnumerator");
        IEnumeratorOfT = typeSystem.GetType("System.Collections.Generic.IEnumerator`1");
        IFormatProvider = typeSystem.GetType("System.IFormatProvider");
        IList = typeSystem.GetType("System.Collections.IList");
        IListOfT = typeSystem.GetType("System.Collections.Generic.IList`1");
        IReadOnlyListOfT = typeSystem.GetType("System.Collections.Generic.IReadOnlyList`1");
        Int32 = typeSystem.GetType("System.Int32");
        IntPtr = typeSystem.GetType("System.IntPtr");
        InvalidCastException = typeSystem.GetType("System.InvalidCastException");
        ListOfT = typeSystem.GetType("System.Collections.Generic.List`1");
        MethodInfo = typeSystem.GetType("System.Reflection.MethodInfo");
        MulticastDelegate = typeSystem.GetType("System.MulticastDelegate");
        NotSupportedException = typeSystem.GetType("System.NotSupportedException");
        NullReferenceException = typeSystem.GetType("System.NullReferenceException");
        NullableT = typeSystem.GetType("System.Nullable`1");
        Object = typeSystem.GetType("System.Object");
        ObsoleteAttribute = typeSystem.GetType("System.ObsoleteAttribute");
        String = typeSystem.GetType("System.String");
        Type = typeSystem.GetType("System.Type");
        Uri = typeSystem.GetType("System.Uri");
        Void = typeSystem.GetType("System.Void");

        _actionOfT = new IXamlType[16];
        for (var i = 1; i <= 16; ++i)
            _actionOfT[i - 1] = typeSystem.GetType($"System.Action`{i}");

        _funcOfT = new IXamlType[17];
        for (var i = 1; i <= 17; ++i)
            _funcOfT[i - 1] = typeSystem.GetType($"System.Func`{i}");

        ExperimentalAttribute = typeSystem.FindType("System.Diagnostics.CodeAnalysis.ExperimentalAttribute");
    }
}
