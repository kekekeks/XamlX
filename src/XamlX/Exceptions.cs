using System;
using System.Xml;
using XamlX.Ast;

namespace XamlX
{
    public class XamlParseException : XmlException
    {
        public XamlParseException(string message, int line, int position) : base(
            $"{message} (line {line} position {position})",
            null, line, position)
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
