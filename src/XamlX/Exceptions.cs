using System;
using XamlX.Ast;

namespace XamlX
{
    public class XamlXParseException : Exception
    {
        public int Line { get; }
        public int Position { get; }
        
        public XamlXParseException(string message, int line, int position) : base($"{message} (line {line} position {position})")
        {
            Line = line;
            Position = position;
        }

        public XamlXParseException(string message, IXamlXLineInfo lineInfo) : this(message, lineInfo.Line, lineInfo.Position)
        {
            
        }
    }

    public class XamlXTransformException : XamlXParseException
    {
        public XamlXTransformException(string message, IXamlXLineInfo lineInfo) : base(message, lineInfo)
        {

        }
    }

    public class XamlXLoadException : XamlXParseException
    {
        public XamlXLoadException(string message, IXamlXLineInfo lineInfo) : base(message, lineInfo)
        {
        }
    }


    public class XamlXTypeSystemException : Exception
    {
        public XamlXTypeSystemException(string message) : base(message)
        {
            
        }
    }
}
