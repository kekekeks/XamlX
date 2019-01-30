using System;

namespace XamlX
{
    public class XamlParseException : Exception
    {
        public XamlParseException(string message, int line, int position) : base($"{line}:{position}: {message}")
        {
            
        }
    }
}