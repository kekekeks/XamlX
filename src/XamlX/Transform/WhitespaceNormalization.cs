using System;
using XamlX.Ast;

namespace XamlX.Transform
{
    // See: https://docs.microsoft.com/en-us/dotnet/desktop/xaml-services/white-space-processing
    internal static class WhitespaceNormalization
    {
        private static readonly char[] Whitespace = new char[] {' ', '\n', '\t'};

        public static bool IsWhitespace(string text)
        {
            foreach (var t in text)
            {
                if (!IsWhitespace(t))
                {
                    return false;
                }
            }

            // NOTE: empty text nodes count as whitespace
            return true;
        }

        private static bool IsWhitespace(char ch)
        {
            return ch == ' ' || ch == '\n' || ch == '\t';
        }
    }
}