using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using XamlX.Ast;

namespace XamlX.Transform
{
    internal static class WhitespaceNormalization
    {
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
            // While the XAML spec does not list \r, the implementation still considers it
            // Usually, the XML parser will already normalize newlines, but one can still
            // insert a character entity (&#x0D;) to bypass it, which will then be caught here.
            return ch == ' ' || ch == '\n' || ch == '\t' || ch == '\r';
        }

        internal static string NormalizeWhitespace(string text, bool trimStart = false, bool trimEnd = false)
        {
            var start = 0;
            if (trimStart)
            {
                for (; start < text.Length && IsWhitespace(text[start]); start++)
                {
                }

                if (start >= text.Length)
                {
                    return string.Empty; // The entire string was whitespace
                }
            }

            // End is exclusive
            var end = text.Length;
            if (trimEnd)
            {
                for (; end > start && IsWhitespace(text[end - 1]); end--)
                {
                }

                if (end <= start)
                {
                    return string.Empty; // The entire string was whitespace
                }
            }

            // 1) Convert all white-space characters into spaces (new-lines become spaces, etc.)
            // 2) Collapse all subsequent white-space characters into a single space
            var result = new StringBuilder(end - start);
            for (var i = start; i < end; i++)
            {
                var ch = text[i];
                if (IsWhitespace(ch))
                {
                    ch = ' '; // All whitespace is normalized to spaces
                    // Consume all whitespace directly following this
                    for (; i + 1 < end && IsWhitespace(text[i + 1]); i++)
                    {
                    }
                }

                result.Append(ch);
            }
            return result.ToString();
        }

        /// <summary>
        /// Applies the whitespace normalization process and content model transformation.
        /// </summary>
        public static void Apply(List<IXamlAstValueNode> contentNodes,
            TransformerConfiguration config)
        {
            bool ShouldTrimWhitespaceAround(int index) => contentNodes[index] is XamlAstObjectNode objectNode
                                                          && config.IsTrimSurroundingWhitespaceElement(
                                                              objectNode.Type.GetClrType());

            for (var i = contentNodes.Count - 1; i >= 0; i--)
            {
                var node = contentNodes[i];

                if (node is XamlAstTextNode textNode)
                {
                    // XAML whitespace normalization can be disabled via xml:space="preserve" on an element or
                    // any of its ancestors.
                    if (!textNode.PreserveWhitespace)
                    {
                        // Trim spaces immediately following the start tag or following a tag that wants surrounding
                        // whitespace trimmed
                        var trimStart = i <= 0 || ShouldTrimWhitespaceAround(i - 1);

                        // Trim spaces immediately preceding the end tag or preceding a tag that wants surrounding
                        // whitespace trimmed
                        var trimEnd = i >= contentNodes.Count - 1 || ShouldTrimWhitespaceAround(i + 1);

                        textNode.Text = NormalizeWhitespace(textNode.Text, trimStart, trimEnd);
                        if (textNode.Text.Length == 0)
                        {
                            // Remove text nodes that have been trimmed in their entirety
                            contentNodes.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public static void RemoveWhitespaceNodes<T>(List<T> nodes) where T : IXamlAstNode
        {
            for (var i = nodes.Count - 1; i >= 0; i--)
            {
                if (ShouldRemoveNode(nodes[i]))
                {
                    nodes.RemoveAt(i);
                }
            }
        }

        private static bool ShouldRemoveNode(IXamlAstNode node)
        {
            switch (node)
            {
                case XamlAstTextNode textChild:
                    { 
                        return IsWhitespace(textChild.Text);
                    }
                case XamlValueWithSideEffectNodeBase sideEffectNode:
                    {
                        return ShouldRemoveNode(sideEffectNode.Value);
                    }
            }

            return false;
        }
    }
}
