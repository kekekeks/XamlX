﻿namespace TypeSystemTest.Models;

public class GenericType<T> : GenericBaseType<T>, IGenericInterface<T>
{
    public T? Field;
    public T? Property { get; set; }
    public T SomeMethod(T x) => x;
}