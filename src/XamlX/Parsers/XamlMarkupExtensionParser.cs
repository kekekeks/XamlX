using System;
using System.Collections.Generic;
using XamlX.Ast;

namespace XamlX.Parsers
{
#if !XAMLX_INTERNAL
    public
#endif
    sealed class XamlMarkupExtensionParser
    {
        
        public class ParseException : Exception
        {
            public ParseException(string message, int position) : base(message + " at position " + position)
            {

            }
        }
        
        class Node
        {
            public string Name { get; set; }
            public List<object> PositionalArguments { get; } = new List<object>();
            public List<(string name, object value)> NamedArguments { get; } = new List<(string name, object value)>();
            
        }

        enum ParserState
        {
            ParsingNodeName,
            ParsingPositionalArgument,
            ParsingNamedArgumentName,
            ParsingNamedArgumentValue,
            RootEnd
        }

        public static XamlAstObjectNode Parse(IXamlLineInfo info, string ext, Func<string, XamlAstXmlTypeReference> typeResolver)
        {
            var root = ParseNode(ext);

            IXamlAstValueNode Convert(object node)
            {
                if (node is string s)
                    return new XamlAstTextNode(info, s);
                var n = (Node) node;

                var type = typeResolver(n.Name);
                type.IsMarkupExtension = true;
                var rv = new XamlAstObjectNode(info, type);
                foreach (var pa in n.PositionalArguments)
                    rv.Arguments.Add(Convert(pa));
                foreach (var arg in n.NamedArguments)
                    rv.Children.Add(new XamlAstXamlPropertyValueNode(info,
                        new XamlAstNamePropertyReference(info, rv.Type, arg.name, rv.Type), Convert(arg.value)));

                return rv;
            }

            return (XamlAstObjectNode) Convert(root);
        }
        
        static Node ParseNode(string ext)
        {
            ext = ext.Trim();
            if (!ext.StartsWith("{") || !ext.EndsWith("}"))
                throw new ArgumentException($"'{ext}' doesn't look like a valid XAML markup extension");
            var root = new Node();
            var current = root;
            var stack = new Stack<(Node, string, ParserState)>();
            stack.Push((null, null, ParserState.RootEnd));
            
            var state = ParserState.ParsingNodeName;

            string argument = null;
            string argumentName = null;

            void NewNode()
            {
                stack.Push((current, argumentName, state));
                current = new Node();
                state = ParserState.ParsingNodeName;
                argument = null;
                argumentName = null;
            }

            void EndNode()
            {
                var finished = current;
                (current, argumentName, state) = stack.Pop();
                if(state != ParserState.RootEnd)
                    FinishArgument(finished);
            }

            void FinishStringArgument() => FinishArgument(argument?.Trim());
            void FinishArgument(object value)
            {
                if (state == ParserState.ParsingPositionalArgument)
                {
                    if (value != null)
                        current.PositionalArguments.Add(value);
                }
                else if (state == ParserState.ParsingNamedArgumentValue)
                {
                    state = ParserState.ParsingNamedArgumentName;
                    current.NamedArguments.Add((argumentName?.Trim(), value));
                }
                else
                    throw new InvalidOperationException();
                argument = null;
                argumentName = null;
            }

            bool insideEscapeSequence = false;
            
            // We've already consumed the first '{' token by creating the root node
            for (var c = 1; c < ext.Length; c++)
            {
                var ch = ext[c];
                var escaped = false;

                if (insideEscapeSequence)
                {
                    escaped = true;
                    insideEscapeSequence = false;
                }
                
                if (!escaped && ch == '\\')
                {
                    insideEscapeSequence = true;
                    continue;
                }
                
                
                if (state == ParserState.RootEnd)
                {
                    if (!char.IsWhiteSpace(ch))
                        throw new ParseException("Invalid character after the end of the extension root node", c);
                }
                else if (state == ParserState.ParsingNodeName)
                {
                    if (!escaped && ch == '{')
                        throw new ParseException("{ is not valid at the current state", c);
                    else if (!escaped && ch == '}')
                    {
                        EndNode();
                    }
                    else if (char.IsWhiteSpace(ch))
                    {
                        state = ParserState.ParsingPositionalArgument;
                        argument = null;
                    }
                    else
                        current.Name += ch;
                }
                else if(state == ParserState.ParsingPositionalArgument 
                        || state == ParserState.ParsingNamedArgumentValue)
                {
                    if (!escaped && ch == '{' && string.IsNullOrWhiteSpace(argument)) 
                        NewNode();
                    else if (!escaped && ch == '}')
                    {
                        FinishStringArgument();
                        EndNode();
                    }
                    else if (!escaped && ch == ',')
                    {
                        FinishStringArgument();
                    }
                    else if (!escaped && ch == '=' && state == ParserState.ParsingPositionalArgument)
                    {
                        argumentName = argument;
                        argument = null;
                        state = ParserState.ParsingNamedArgumentValue;
                    }
                    else
                        argument += ch;
                }
                else if(state == ParserState.ParsingNamedArgumentName)
                {
                    // Either after , or } from the previous argument value
                    if (argumentName == null && !escaped && ch == '}')
                    {
                        EndNode();
                    }
                    else if (!escaped && (ch == '{' || ch == '}'))
                        throw new ParseException($"{ch} is not valid at the current state", c);
                    else if (!escaped && ch == '=')
                        state = ParserState.ParsingNamedArgumentValue;
                    else if(string.IsNullOrEmpty(argumentName)
                        && (ch== ',' || char.IsWhiteSpace(ch)))
                    {
                        // Do nothing, it's whitespace
                    }
                    else
                        argumentName += ch;
                }   
            }

            if (state != ParserState.RootEnd)
                throw new ParseException("Unbalanced { and }", ext.Length - 1);

            return root;
        }
    }
}
