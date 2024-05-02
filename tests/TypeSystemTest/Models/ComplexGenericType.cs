namespace TypeSystemTest.Models;

public class ComplexGenericType<T>
{
    public T Do<TArg>(TArg arg, int i)
    {
        return default(T);
    }
}
