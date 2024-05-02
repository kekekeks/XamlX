using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeSystemTest.Models;
using XamlX.TypeSystem;
using Xunit;

namespace TypeSystemTest;

public abstract class BaseTest
{
    private readonly IXamlTypeSystem _typeSystem;
    private static readonly Dictionary<string, string> _cleanedFullName = [];

    /// <summary>
    /// See GitHub Issue https://github.com/kekekeks/XamlX/issues/119
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    private static string GetNormalizzeFullName(string fullName)
    {
        if (!_cleanedFullName.TryGetValue(fullName, out var cleanedName))
        {
            cleanedName = fullName;
            var index = cleanedName.IndexOf('`');
            if (index > -1)
            {
                var fullNameLen = fullName.Length;
                var sb = new StringBuilder(fullNameLen);
                var numberOfArgs = 0;
                int i = 1;
                for (; index + i < fullNameLen && char.IsNumber(fullName[index + i]); i++)
                {
                    numberOfArgs = (numberOfArgs * (int)Math.Pow(i, 10)) + (fullName[index + i] - 48);
                }
                sb.Append(fullName, 0, index + i);
                var typeArgStart = index + i + 1;
                if (numberOfArgs > 0 && typeArgStart < fullNameLen)
                {
                    sb.Append('<');
                    var argIndex = 0;
                    var skip = false;
                    var parsingArg = false;
                    for (i = typeArgStart; argIndex < numberOfArgs && i < fullNameLen; i++)
                    {
                        var c = fullName[i];
                        switch (c)
                        {
                            case '[' when !parsingArg:
                                parsingArg = true;
                                skip = false;
                                break;
                            case ',' when parsingArg && !skip:
                                argIndex++;
                                sb.Append(',');
                                skip = true;
                                break;
                            case ',' when !parsingArg:
                                skip = true;
                                break;
                            case ']' when skip:
                                skip = false;
                                parsingArg = false;
                                break;
                            default:
                                if (!skip)
                                {
                                    sb.Append(c);
                                }
                                break;
                        }
                    }
                    sb[sb.Length - 1] = '>';
                }
                cleanedName = sb.ToString();
            }
            _cleanedFullName[fullName] = cleanedName;
        }
        return cleanedName;
    }

    protected BaseTest(IXamlTypeSystem typeSystem)
    {
        _typeSystem = typeSystem;
    }

    protected IXamlTypeSystem TypeSystem => _typeSystem;

    [Fact]
    public void Generic_Types_Are_Correctly_Handled()
    {
        var wut = TypeSystem.GetType("TypeSystemTest.Models.TestType");
        var ts = TypeSystem.GetType("System.String");
        var f = wut.Fields.First(x => x.Name == nameof(TestType.GenericStringField));
        Assert.Equal("TypeSystemTest.Models.GenericType`1<System.String>", GetNormalizzeFullName(f.FieldType.FullName));
        Assert.Equal("TypeSystemTest.Models.GenericBaseType`1<System.String>", GetNormalizzeFullName(f.FieldType.BaseType.FullName));
        var iface = f.FieldType.Interfaces.First(x => x.Name == "IGenericInterface`1");
        Assert.Equal("TypeSystemTest.Models.IGenericInterface`1<System.String>", GetNormalizzeFullName(iface.FullName));

        var m = f.FieldType.Methods.First(x => x.Name == "SomeMethod");
        Assert.Equal("System.String", m.ReturnType.FullName);
        Assert.Equal("System.String", m.Parameters[0].FullName);
        Assert.Empty(f.FieldType.Methods.First(x => x.Name == "SomeMethod").Parameters[0].GenericArguments);

        var gf = f.FieldType.Fields.First(x => x.Name == "Field");
        Assert.Equal("System.String", gf.FieldType.FullName);

        var p = f.FieldType.Properties.First(x => x.Name == "Property");
        Assert.Equal("System.String", p.PropertyType.FullName);
        Assert.Equal("System.String", p.Getter.ReturnType.FullName);
        Assert.Equal("System.String", p.Setter.Parameters[0].FullName);

        var gm_1 = wut.FindMethod(m => m.IsGenericMethodDefinition && m.Name == nameof(TestType.Sub) && m.GenericParameters.Count == 1);
        
        Assert.NotNull(gm_1);
        Assert.Empty(gm_1.GenericArguments);
        Assert.True(gm_1.ContainsGenericParameters);
        Assert.Equal("T", gm_1.Parameters[0].Name);

        var cgm_1 = gm_1.MakeGenericMethod([ts]);

        Assert.NotNull(cgm_1);
        Assert.False(cgm_1.ContainsGenericParameters);
        Assert.False(cgm_1.IsGenericMethodDefinition);
        Assert.Empty(cgm_1.GenericParameters);
        Assert.Single(cgm_1.GenericArguments);
        Assert.Equal("System.String", GetNormalizzeFullName(cgm_1.Parameters[0].FullName));

        var wutGenericClass = TypeSystem.GetType("TypeSystemTest.Models.ComplexGenericType`1");

        Assert.NotNull(wutGenericClass);

        Assert.Single(wutGenericClass.GenericParameters);

        Assert.Equal("T", wutGenericClass.GenericParameters[0].Name);


        Assert.True(wutGenericClass.Methods[0].ContainsGenericParameters);
        Assert.Equal(2, wutGenericClass.Methods[0].Parameters.Count);
        Assert.Equal("TArg", wutGenericClass.Methods[0].Parameters[0].Name);
        Assert.Equal("Int32", wutGenericClass.Methods[0].Parameters[1].Name);
        Assert.Equal("T", wutGenericClass.Methods[0].ReturnType.Name);
        Assert.Single(wutGenericClass.Methods[0].GenericParameters);
        Assert.Empty(wutGenericClass.Methods[0].GenericArguments);

        var gi = wutGenericClass.MakeGenericType(ts);

        Assert.True(gi.Methods[0].ContainsGenericParameters);
        Assert.Equal(2, gi.Methods[0].Parameters.Count);
        Assert.Equal("TArg", gi.Methods[0].Parameters[0].Name);
        Assert.Equal("Int32", gi.Methods[0].Parameters[1].Name);
        Assert.Equal("String", gi.Methods[0].ReturnType.Name);
        Assert.Single(gi.Methods[0].GenericParameters);
        Assert.Empty(gi.Methods[0].GenericArguments);

        var gim = gi.Methods[0].MakeGenericMethod([ts]);

        Assert.False(gim.ContainsGenericParameters);
        Assert.Equal(2, gim.Parameters.Count);
        Assert.Equal("String", gim.Parameters[0].Name);
        Assert.Equal("Int32", gim.Parameters[1].Name);
        Assert.Equal("String", gim.ReturnType.Name);
        Assert.Empty(gim.GenericParameters);
        Assert.Single(gim.GenericArguments);
    }

    [Fact]
    public void ArrayElementType_Is_Correctly_Handled()
    {
        var wut = TypeSystem.GetType("TypeSystemTest.Models.TestType");
        var af = wut.Fields.First(x => x.Name == nameof(TestType.ArrayElementType));
        Assert.True(af.FieldType?.IsArray);
        Assert.Equal("System.String", af.FieldType.ArrayElementType?.FullName);
    }

    [Fact]
    public void Dictionary_Not_Assignable_From_String()
    {
        var stringType = TypeSystem.FindType("System.String");
        var dictBase = TypeSystem.GetType("System.Collections.Generic.IDictionary`2");
        var stringStringDict = dictBase.MakeGenericType(stringType, stringType);

        Assert.False(stringStringDict.IsAssignableFrom(stringType));
    }

    [Fact]
    public void Generic_FullName_Is_Normalizzed()
    {
        var ts = TypeSystem.GetType("System.String");
        Assert.Equal("System.String", GetNormalizzeFullName(ts.FullName));
        var tl = TypeSystem.GetType("System.Collections.Generic.List`1");
        Assert.Equal("System.Collections.Generic.List`1", GetNormalizzeFullName(tl.FullName));
        var ls = tl.MakeGenericType(ts);
        Assert.Equal("System.Collections.Generic.List`1<System.String>", GetNormalizzeFullName(ls.FullName));
    }
}
