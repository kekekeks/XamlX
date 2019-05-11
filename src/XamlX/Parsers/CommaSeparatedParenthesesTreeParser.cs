using System;
using System.Collections.Generic;

namespace XamlX.Parsers
{
#if !XAMLX_INTERNAL
    public
#endif
    sealed class CommaSeparatedParenthesesTreeParser
    {
        public class Node
        {
            public string Value { get; set; }
            public List<Node> Children { get; set; } = new List<Node>();
        }

        public class ParseException : Exception
        {
            public ParseException(string message, int position) : base(message + " at position " + position)
            {

            }
        }

        public static List<Node> Parse(string s)
        {
            var stack = new Stack<Node>();
            var current = new Node();
            //Initial parser state with initial node added to the root parent
            stack.Push(current);
            current = new Node();
            stack.Peek().Children.Add(current);

            bool afterClosing = false;
            for (var c = 0; c < s.Length; c++)
            {
                var ch = s[c];
                if (ch == ',')
                {
                    stack.Peek().Children.Add(current = new Node());
                }
                else if(afterClosing && char.IsWhiteSpace(ch))
                    continue;
                else if (ch == ')')
                {
                    current = stack.Pop();
                    if (stack.Count == 0)
                        throw new ParseException("Unmatched ')'", c);
                }
                else if (afterClosing)
                    throw new ParseException("Invalid character after ')'", c);
                else if(ch=='(')
                {
                    stack.Push(current);
                    stack.Peek().Children.Add(current = new Node());
                }
                else
                {
                    current.Value += ch;
                }

                afterClosing = ch == ')';
            }

            // Final state: initial node at the top of the stack
            return stack.Pop().Children;
        }
    }
    
}
