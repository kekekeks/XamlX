using System;
using System.Xml;
using XamlIl.Ast;

namespace XamlIl
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlParseException : XmlException
    {
        public XamlIlParseException(string message, int line, int position) : base(
            $"{message} (line {line} position {position})",
            null, line, position)
        {
        }

        public XamlIlParseException(string message, IXamlIlLineInfo lineInfo) : this(message, lineInfo.Line, lineInfo.Position)
        {
            
        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlTransformException : XamlIlParseException
    {
        public XamlIlTransformException(string message, IXamlIlLineInfo lineInfo) : base(message, lineInfo)
        {

        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlLoadException : XamlIlParseException
    {
        public XamlIlLoadException(string message, IXamlIlLineInfo lineInfo) : base(message, lineInfo)
        {
        }
    }


#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlTypeSystemException : Exception
    {
        public XamlIlTypeSystemException(string message) : base(message)
        {
            
        }
    }
}
