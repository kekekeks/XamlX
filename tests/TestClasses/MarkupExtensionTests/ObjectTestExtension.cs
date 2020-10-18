namespace XamlParserTests
{
    public class ObjectTestExtension
    {
        public object Returned { get; set; }

        public object ProvideValue()
        {
            return Returned;
        }
    }
}
