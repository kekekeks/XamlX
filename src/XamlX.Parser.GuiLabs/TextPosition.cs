using Microsoft.Language.Xml;
using XamlX.Ast;

namespace XamlX.Parsers
{
#if !XAMLX_INTERNAL
    public
#endif
    class TextPosition : IXamlLineInfo
    {
        private readonly SyntaxNode _node;

        public TextPosition(IXmlElement element, string xml)
            : this((SyntaxNode)element, xml)
        {
        }

        public TextPosition(SyntaxNode node, string xml)
        {
            _node = node;
            (Line, Position) = GetLineAndPosition(_node.Span.Start, xml);
        }

        public int Line { get; set; }
        public int Position { get; set; }
        public int SpanStart => _node.Span.Start;
        public int SpanEnd => _node.Span.End;
        public object XmlNode => _node;

        private static (int line, int position) GetLineAndPosition(int offset, string xml)
        {
            var character = 0;
            var line = 1;

            if (offset != 0 && xml.Length < offset)
            {
                offset = xml.Length - 1;
            }

            void NewLine()
            {
                line++;
                character = 0;
            }

            var i = 0;
            for (; i < offset; i++)
            {
                if (xml[i] == '\n')
                {
                    NewLine();
                }
                else if (xml[i] == '\r')
                {
                    bool hasRn = i + 1 < xml.Length && xml[i + 1] == '\n';
                    if (hasRn)
                    {
                        i++;
                    }
                    NewLine();
                }
                else
                {
                    character++;
                }
            }

            return (line, character);
        }
    }
}
