using System;
using System.Collections.Generic;
using System.Text;
using XamlX.TypeSystem;

namespace TypeSystemTest;

public abstract partial class BaseTest
{
    private readonly IXamlTypeSystem _typeSystem;
    private static readonly Dictionary<string, string> _cleanedFullName = [];
    protected const string ModelsNamespace = $"{nameof(TypeSystemTest)}.{nameof(TypeSystemTest.Models)}";

    /// <summary>
    /// See GitHub Issue https://github.com/kekekeks/XamlX/issues/119
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    protected static string? GetNormalizedFullName(string? fullName)
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
}
