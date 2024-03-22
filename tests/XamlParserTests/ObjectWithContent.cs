namespace XamlParserTests
{
    internal sealed class ObjectWithContent
    {
        public int Id { get; set; }

        [Content]
        public object ChildContent { get; set; }
    }
}
