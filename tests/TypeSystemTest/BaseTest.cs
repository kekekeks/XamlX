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
    private static string? GetNormalizedFullName(string? fullName)
    {
        if (fullName is null)
        {
            return null;
        }
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
        var testTypeType = TypeSystem.GetType("TypeSystemTest.Models.TestType");
        var stringType = TypeSystem.GetType("System.String");
        var genericStringField = testTypeType.Fields.First(x => x.Name == nameof(TestType.GenericStringField));
        Assert.Equal("TypeSystemTest.Models.GenericType`1<System.String>", GetNormalizedFullName(genericStringField?.FieldType.FullName));
        Assert.Equal("TypeSystemTest.Models.GenericBaseType`1<System.String>", GetNormalizedFullName(genericStringField?.FieldType.BaseType?.FullName));
        var genericInterfceType = genericStringField?.FieldType.Interfaces.First(x => x.Name == "IGenericInterface`1");
        Assert.Equal("TypeSystemTest.Models.IGenericInterface`1<System.String>", GetNormalizedFullName(genericInterfceType?.FullName));

        var genericSomeMethod = genericStringField!.FieldType.Methods.First(x => x.Name == "SomeMethod");
        Assert.Equal("System.String", genericSomeMethod.ReturnType.FullName);
        Assert.Equal("System.String", genericSomeMethod.Parameters[0].FullName);
        Assert.Empty(genericStringField.FieldType.Methods.First(x => x.Name == "SomeMethod").Parameters[0].GenericArguments);

        var gf = genericStringField.FieldType.Fields.First(x => x.Name == "Field");
        Assert.Equal("System.String", gf.FieldType.FullName);

        var p = genericStringField.FieldType.Properties.First(x => x.Name == "Property");
        Assert.Equal("System.String", p.PropertyType.FullName);
        Assert.Equal("System.String", p.Getter?.ReturnType.FullName);
        Assert.Equal("System.String", p.Setter?.Parameters[0].FullName);

        var genericSubMethodGenericDefinition = testTypeType.FindMethod(m => m.IsGenericMethodDefinition && m.Name == nameof(TestType.Sub) && m.GenericParameters.Count == 1);
        
        Assert.NotNull(genericSubMethodGenericDefinition);
        Assert.Empty(genericSubMethodGenericDefinition.GenericArguments);
        Assert.True(genericSubMethodGenericDefinition.ContainsGenericParameters);
        Assert.Equal("T", genericSubMethodGenericDefinition.Parameters[0].Name);

        var concreteSubMethod = genericSubMethodGenericDefinition.MakeGenericMethod([stringType]);

        Assert.NotNull(concreteSubMethod);
        Assert.False(concreteSubMethod.ContainsGenericParameters);
        Assert.False(concreteSubMethod.IsGenericMethodDefinition);
        Assert.Empty(concreteSubMethod.GenericParameters);
        Assert.Single(concreteSubMethod.GenericArguments);
        Assert.Equal("System.String", GetNormalizedFullName(concreteSubMethod.Parameters[0].FullName));

        /*
        public class ComplexGenericType<T>
        {
            public T Do<TArg>(TArg arg, int i)
            {
                return default(T);
            }
        }
        */
        var complexGenericTypeDefinition = TypeSystem.GetType("TypeSystemTest.Models.ComplexGenericType`1");

        Assert.NotNull(complexGenericTypeDefinition);

        Assert.Single(complexGenericTypeDefinition.GenericParameters);

        Assert.Equal("T", complexGenericTypeDefinition.GenericParameters[0].Name);


        Assert.True(complexGenericTypeDefinition.Methods[0].ContainsGenericParameters);
        Assert.Equal(2, complexGenericTypeDefinition.Methods[0].Parameters.Count);
        Assert.Equal("TArg", complexGenericTypeDefinition.Methods[0].Parameters[0].Name);
        Assert.Equal("Int32", complexGenericTypeDefinition.Methods[0].Parameters[1].Name);
        Assert.Equal("T", complexGenericTypeDefinition.Methods[0].ReturnType.Name);
        Assert.Single(complexGenericTypeDefinition.Methods[0].GenericParameters);
        Assert.Empty(complexGenericTypeDefinition.Methods[0].GenericArguments);

        var concreteComplexType = complexGenericTypeDefinition.MakeGenericType(stringType);

        Assert.True(concreteComplexType.Methods[0].ContainsGenericParameters);

        IXamlMethod genericDoMethodDefinition = concreteComplexType.Methods[0];
        Assert.Equal(2, genericDoMethodDefinition.Parameters.Count);
        Assert.Equal("TArg", genericDoMethodDefinition.Parameters[0].Name);
        Assert.Equal("Int32", genericDoMethodDefinition.Parameters[1].Name);
        Assert.Equal("String", genericDoMethodDefinition.ReturnType.Name);
        Assert.Single(genericDoMethodDefinition.GenericParameters);
        Assert.Empty(genericDoMethodDefinition.GenericArguments);

        var concreteDoMethod = concreteComplexType.Methods[0].MakeGenericMethod([stringType]);

        Assert.False(concreteDoMethod.ContainsGenericParameters);
        Assert.Equal(2, concreteDoMethod.Parameters.Count);
        Assert.Equal("String", concreteDoMethod.Parameters[0].Name);
        Assert.Equal("Int32", concreteDoMethod.Parameters[1].Name);
        Assert.Equal("String", concreteDoMethod.ReturnType.Name);
        Assert.Empty(concreteDoMethod.GenericParameters);
        Assert.Single(concreteDoMethod.GenericArguments);
    }

    [Fact]
    public void ArrayElementType_Is_Correctly_Handled()
    {
        var wut = TypeSystem.GetType("TypeSystemTest.Models.TestType");
        var af = wut.Fields.First(x => x.Name == nameof(TestType.ArrayElementType));
        Assert.True(af?.FieldType.IsArray);
        Assert.Equal("System.String", af!.FieldType.ArrayElementType?.FullName);
    }

    [Fact]
    public void Dictionary_Not_Assignable_From_String()
    {
        var stringType = TypeSystem.GetType("System.String");
        var dictBase = TypeSystem.GetType("System.Collections.Generic.IDictionary`2");
        var stringStringDict = dictBase.MakeGenericType(stringType, stringType);

        Assert.False(stringStringDict.IsAssignableFrom(stringType));
    }

    [Fact]
    public void Generic_FullName_Is_Normalizzed()
    {
        var ts = TypeSystem.GetType("System.String");
        Assert.Equal("System.String", GetNormalizedFullName(ts.FullName));
        var tl = TypeSystem.GetType("System.Collections.Generic.List`1");
        Assert.Equal("System.Collections.Generic.List`1", GetNormalizedFullName(tl.FullName));
        var ls = tl.MakeGenericType(ts);
        Assert.Equal("System.Collections.Generic.List`1<System.String>", GetNormalizedFullName(ls.FullName));
    }
}
