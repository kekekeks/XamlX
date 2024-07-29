using System.Linq;
using TypeSystemTest.Models.Generics;
using XamlX.TypeSystem;
using Xunit;


namespace TypeSystemTest;

partial class BaseTest
{
    protected const string GenericsNamespace = $"{ModelsNamespace}.{nameof(Models.Generics)}";
    [Fact]
    public void Generic_Types_Are_Correctly_Handled()
    {
        var testTypeType = TypeSystem.GetType($"{GenericsNamespace}.TestType");
        var stringType = TypeSystem.GetType("System.String");
        var genericStringField = testTypeType.Fields.First(x => x.Name == nameof(TestType.GenericStringField));
        Assert.Equal($"{GenericsNamespace}.GenericType`1<System.String>", GetNormalizedFullName(genericStringField?.FieldType.FullName));
        Assert.Equal($"{GenericsNamespace}.GenericBaseType`1<System.String>", GetNormalizedFullName(genericStringField?.FieldType.BaseType?.FullName));
        var genericInterfceType = genericStringField?.FieldType.Interfaces.First(x => x.Name == "IGenericInterface`1");
        Assert.Equal($"{GenericsNamespace}.IGenericInterface`1<System.String>", GetNormalizedFullName(genericInterfceType?.FullName));

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
        var complexGenericTypeDefinition = TypeSystem.GetType($"{GenericsNamespace}.ComplexGenericType`1");

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
    public void Generic_ArrayElementType_Is_Correctly_Handled()
    {
        var wut = TypeSystem.GetType($"{GenericsNamespace}.TestType");
        var af = wut.Fields.First(x => x.Name == nameof(TestType.ArrayElementType));
        Assert.True(af?.FieldType.IsArray);
        Assert.Equal("System.String", af!.FieldType.ArrayElementType?.FullName);
    }

    [Fact]
    public void Generic_Dictionary_Not_Assignable_From_String()
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
