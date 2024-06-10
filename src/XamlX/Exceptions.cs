using System;
using System.Xml;
using XamlX.Ast;

namespace XamlX
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlParseException : XmlException
    {
        public XamlParseException(string message, int line, int position, Exception? innerException = null)
            : base(message, innerException, line, position)
        {
        }

        public XamlParseException(string message, IXamlLineInfo? lineInfo, Exception? innerException = null)
            : this(message, lineInfo?.Line ?? 0, lineInfo?.Position ?? 0, innerException)
        {

        }

        public string? Document { get; init; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlTransformException : XamlParseException
    {
        public XamlTransformException(string message, IXamlLineInfo? lineInfo, Exception? innerException = null)
            : base(message, lineInfo, innerException)
        {

        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlLoadException : XamlParseException
    {
        public XamlLoadException(string message, IXamlLineInfo? lineInfo, Exception? innerException = null)
            : base(message, lineInfo, innerException)
        {
        }
    }


#if !XAMLX_INTERNAL
    public
#endif
    class XamlTypeSystemException : Exception
    {
        public XamlTypeSystemException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }
}