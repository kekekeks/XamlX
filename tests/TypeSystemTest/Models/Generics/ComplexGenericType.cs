namespace TypeSystemTest.Models.Generics;

public class ComplexGenericType<T>
{
    public T? Do<TArg>(TArg arg, int i)
    {
        return default;
    }
}
