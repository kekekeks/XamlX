namespace TypeSystemTest.Models.Generics;

public class TestType
{
    public GenericType<string>? GenericStringField;
    public string[]? ArrayElementType;
    public void Sub<T>(T obj)
    {

    }

    public void Sub<T, T2>(T obj, T2 t)
        where T2 : struct
    {

    }


    public void Sub()
    {

    }
}
