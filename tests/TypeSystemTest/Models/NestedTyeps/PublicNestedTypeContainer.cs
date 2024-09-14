namespace TypeSystemTest.Models.NestedTyeps;

public static class PublicNestedTypeContainer
{
    public class PublicNestedType
    {
        public string Value { get; } = "Hello World";
    }

    internal class InternalNestedType
    {
        public string Value { get; } = "Hello World";
    }

    private class PrivateNestedType
    {
        public string Value { get; } = "Hello World";
    }
}
