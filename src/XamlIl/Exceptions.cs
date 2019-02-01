using System;
using XamlIl.Ast;

namespace XamlIl
{
    public class XamlIlParseException : Exception
    {
        public XamlIlParseException(string message, int line, int position) : base($"{line}:{position}: {message}")
        {
            
        }

        public XamlIlParseException(string message, IXamlIlLineInfo lineInfo) : this(message, lineInfo.Line, lineInfo.Position)
        {
            
        }
    }

    public class XamlIlTransformException : XamlIlParseException
    {
        public XamlIlTransformException(string message, IXamlIlLineInfo lineInfo) : base(message, lineInfo)
        {

        }
    }

    public class XamlIlLoadException : XamlIlParseException
    {
        public XamlIlLoadException(string message, IXamlIlLineInfo lineInfo) : base(message, lineInfo)
        {
        }
    }


    public class XamlIlTypeSystemException : Exception
    {
        public XamlIlTypeSystemException(string message) : base(message)
        {
            
        }
    }
}