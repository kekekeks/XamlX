using System;
using XamlX.Ast;

namespace XamlX
{
    public class XamlParseException : Exception
    {
        public XamlParseException(string message, int line, int position) : base($"{message} (line {line} position {position})")
        {
            
        }

        public XamlParseException(string message, IXamlLineInfo lineInfo) : this(message, lineInfo.Line, lineInfo.Position)
        {
            
        }
    }

    public class XamlTransformException : XamlParseException
    {
        public XamlTransformException(string message, IXamlLineInfo lineInfo) : base(message, lineInfo)
        {

        }
    }

    public class XamlLoadException : XamlParseException
    {
        public XamlLoadException(string message, IXamlLineInfo lineInfo) : base(message, lineInfo)
        {
        }
    }


    public class XamlTypeSystemException : Exception
    {
        public XamlTypeSystemException(string message) : base(message)
        {
            
        }
    }
}