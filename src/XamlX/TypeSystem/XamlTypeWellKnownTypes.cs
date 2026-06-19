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

        // We can't use for-loop below for action/func initialization,
        // because `GetType` needs a const value to ensure this type is not trimmed. 

        var actionOfT = new IXamlType[16];
        actionOfT[0] = typeSystem.GetType("System.Action`1");
        actionOfT[1] = typeSystem.GetType("System.Action`2");
        actionOfT[2] = typeSystem.GetType("System.Action`3");
        actionOfT[3] = typeSystem.GetType("System.Action`4");
        actionOfT[4] = typeSystem.GetType("System.Action`5");
        actionOfT[5] = typeSystem.GetType("System.Action`6");
        actionOfT[6] = typeSystem.GetType("System.Action`7");
        actionOfT[7] = typeSystem.GetType("System.Action`8");
        actionOfT[8] = typeSystem.GetType("System.Action`9");
        actionOfT[9] = typeSystem.GetType("System.Action`10");
        actionOfT[10] = typeSystem.GetType("System.Action`11");
        actionOfT[11] = typeSystem.GetType("System.Action`12");
        actionOfT[12] = typeSystem.GetType("System.Action`13");
        actionOfT[13] = typeSystem.GetType("System.Action`14");
        actionOfT[14] = typeSystem.GetType("System.Action`15");
        actionOfT[15] = typeSystem.GetType("System.Action`16");
        _actionOfT = actionOfT;

        var funcOfT = new IXamlType[17];
        funcOfT[0] = typeSystem.GetType("System.Func`1");
        funcOfT[1] = typeSystem.GetType("System.Func`2");
        funcOfT[2] = typeSystem.GetType("System.Func`3");
        funcOfT[3] = typeSystem.GetType("System.Func`4");
        funcOfT[4] = typeSystem.GetType("System.Func`5");
        funcOfT[5] = typeSystem.GetType("System.Func`6");
        funcOfT[6] = typeSystem.GetType("System.Func`7");
        funcOfT[7] = typeSystem.GetType("System.Func`8");
        funcOfT[8] = typeSystem.GetType("System.Func`9");
        funcOfT[9] = typeSystem.GetType("System.Func`10");
        funcOfT[10] = typeSystem.GetType("System.Func`11");
        funcOfT[11] = typeSystem.GetType("System.Func`12");
        funcOfT[12] = typeSystem.GetType("System.Func`13");
        funcOfT[13] = typeSystem.GetType("System.Func`14");
        funcOfT[14] = typeSystem.GetType("System.Func`15");
        funcOfT[15] = typeSystem.GetType("System.Func`16");
        funcOfT[16] = typeSystem.GetType("System.Func`17");
        _funcOfT = funcOfT;

        ExperimentalAttribute = typeSystem.FindType("System.Diagnostics.CodeAnalysis.ExperimentalAttribute");
    }
}
