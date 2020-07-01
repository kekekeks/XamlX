using System;
using System.Collections.Generic;
using XamlX.Ast;

namespace XamlX.Parsers.SystemXamlMarkupExtensionParser
{
#if !XAMLX_INTERNAL
    public
#endif
    class SystemXamlMarkupExtensionParser
    {
        public static IXamlAstValueNode Parse(IXamlLineInfo li, string ext,
            Func<string, XamlAstXmlTypeReference> typeResolver)
        {
            var ctx = new MeScannerContext(typeResolver, li);
            var scanner = new MeScanner(ctx, ext, li.Line, li.Position);

            var currentTypeStack = new Stack<MeScannerTypeName>();
            IXamlAstValueNode ReadExtension()
            {
                if (scanner.Token != MeTokenType.Open)
                    throw new MeScannerParseException("Unexpected token " + scanner.Token);
                scanner.Read();
                if (scanner.Token != MeTokenType.TypeName)
                    throw new MeScannerParseException("Unexpected token " + scanner.Token);
                var extType = scanner.TokenType;
                
                extType.TypeReference.IsMarkupExtension = true;
                currentTypeStack.Push(ctx.CurrentType);
                ctx.CurrentType = extType;

                var rv = new XamlAstObjectNode(li, extType.TypeReference);
                
                
                while (true)
                {
                    scanner.Read();
                    if (scanner.Token == MeTokenType.Close)
                        break;
                    else if (scanner.Token == MeTokenType.PropertyName)
                    {
                        var prop = scanner.TokenProperty;
                        scanner.Read();
                        if (scanner.Token != MeTokenType.EqualSign)
                            throw new MeScannerParseException("Unexpected token " + scanner.Token);
                        var propValue = Read();
                        rv.Children.Add(new XamlAstXamlPropertyValueNode(li, prop, propValue));
                    }
                    else if (scanner.Token == MeTokenType.String || scanner.Token == MeTokenType.QuotedMarkupExtension
                                                                 || scanner.Token == MeTokenType.Open)
                    {
                        if (rv.Children.Count != 0)
                            throw new MeScannerParseException("Unexpected token after property list " + scanner.Token);
                        rv.Arguments.Add(ReadCurrent());
                    }
                    else if (scanner.Token == MeTokenType.Comma)
                    {
                        continue;
                    }
                    else
                        throw new MeScannerParseException("Unexpected token " + scanner.Token);
                }

                ctx.CurrentType = currentTypeStack.Pop();
                return rv;
            }

            IXamlAstValueNode Read()
            {
                scanner.Read();
                return ReadCurrent();
            }
            
            IXamlAstValueNode ReadCurrent()
            {
                
                if (scanner.Token == MeTokenType.String)
                    return new XamlAstTextNode(li, scanner.TokenText);
                if (scanner.Token == MeTokenType.Open)
                    return ReadExtension();
                if (scanner.Token == MeTokenType.QuotedMarkupExtension)
                    return Parse(li, scanner.TokenText, typeResolver);
                throw new MeScannerParseException("Unexpected token " + scanner.Token);
            }


            return Read();
        }
    }
}
