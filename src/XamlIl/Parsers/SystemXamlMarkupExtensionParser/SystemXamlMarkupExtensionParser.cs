using System;
using System.Collections.Generic;
using XamlIl.Ast;

namespace XamlIl.Parsers.SystemXamlMarkupExtensionParser
{
#if !XAMLIL_INTERNAL
    public
#endif
    class SystemXamlMarkupExtensionParser
    {



        
        public static IXamlIlAstValueNode Parse(IXamlIlLineInfo li, string ext,
            Func<string, XamlIlAstXmlTypeReference> typeResolver)
        {
            var ctx = new MeScannerContext(typeResolver, li);
            var scanner = new MeScanner(ctx, ext, li.Line, li.Position);

            var currentTypeStack = new Stack<MeScannerTypeName>();
            IXamlIlAstValueNode ReadExtension()
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

                var rv = new XamlIlAstObjectNode(li, extType.TypeReference);
                
                
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
                        rv.Children.Add(new XamlIlAstXamlPropertyValueNode(li, prop, propValue));
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

            IXamlIlAstValueNode Read()
            {
                scanner.Read();
                return ReadCurrent();
            }
            
            IXamlIlAstValueNode ReadCurrent()
            {
                
                if (scanner.Token == MeTokenType.String)
                    return new XamlIlAstTextNode(li, scanner.TokenText);
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
