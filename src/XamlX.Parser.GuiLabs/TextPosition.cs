using XamlX.Ast;

namespace XamlX.Parsers
{
#if !XAMLX_INTERNAL
    public
#endif
    class Position : IXamlLineInfo
    {
        public Position(int line, int character, int spanStart, int spanEnd)
        {
            Line = line;
            Character = character;
            SpanStart = spanStart;
            SpanEnd = spanEnd;
        }

        public int Line { get; set; }

        public int Character { get; set; }

        public int SpanStart { get; set; }

        public int SpanEnd { get; set; }

        int IXamlLineInfo.Position { get => Character; set => Character = value; }

        public static Position SpanToPosition(int start, int end, string data)
        {
            int character = 0;
            int line = 0;
            var offset = start + 1;

            if (offset != 0 && data.Length < offset)
            {
                offset = data.Length - 1;
            }

            void NewLine()
            {
                line++;
                character = 0;
            }

            int i = 0;
            for (; i < offset; i++)
            {
                if (data[i] == '\n')
                {
                    NewLine();
                }
                else if (data[i] == '\r')
                {
                    bool hasRn = i + 1 < data.Length && data[i + 1] == '\n';
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

            return new Position(line, character, start, end);
        }
    }
}
