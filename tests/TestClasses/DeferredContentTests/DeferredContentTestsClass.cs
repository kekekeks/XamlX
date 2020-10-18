namespace XamlParserTests
{
    public class DeferredContentTestsClass
    {
        [Content, DeferredContent]
        public object DeferredContent { get; set; }

        public object ObjectProperty { get; set; }
    }
}
