using XamlX.Ast;

namespace XamlParserTests
{
    public class NullLineInfo : IXamlLineInfo
    {
        public int Line { get; set; } = 1;
        public int Position { get; set; } = 1;
    }
}
