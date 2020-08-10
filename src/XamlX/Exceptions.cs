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
        public XamlParseException(string message, int line, int position) : base(
            message, null, line, position)
        {
        }

        public XamlParseException(string message, IXamlLineInfo lineInfo) : this(message, lineInfo.Line, lineInfo.Position)
        {
            
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlTransformException : XamlParseException
    {
        public XamlTransformException(string message, IXamlLineInfo lineInfo) : base(message, lineInfo)
        {

        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlLoadException : XamlParseException
    {
        public XamlLoadException(string message, IXamlLineInfo lineInfo) : base(message, lineInfo)
        {
        }
    }


#if !XAMLX_INTERNAL
    public
#endif
    class XamlTypeSystemException : Exception
    {
        public XamlTypeSystemException(string message) : base(message)
        {
            
        }
    }
}
