using System;
using System.Collections.Generic;
using XamlIl.Ast;

namespace XamlIl.Parsers.SystemXamlMarkupExtensionParser
{
    class MeScannerContext
    {
        private readonly Func<string, XamlIlAstXmlTypeReference> _typeResolver;
        private readonly IXamlIlLineInfo _lineInfo;

        public MeScannerContext(Func<string, XamlIlAstXmlTypeReference> typeResolver, IXamlIlLineInfo lineInfo)
        {
            _typeResolver = typeResolver;
            _lineInfo = lineInfo;
            CurrentType = new MeScannerTypeName(new XamlIlAstXmlTypeReference(lineInfo, "invalid", "invalid"));
        }
        
        public MeScannerBracketModeParseParameters CurrentBracketModeParseParameters { get; }
            = new MeScannerBracketModeParseParameters();

        public MeScannerTypeName CurrentType { get; set; }
        public MeScannerContext FindNamespaceByPrefix => this;

        public Func<string, XamlIlAstXmlTypeReference> TypeResolver => _typeResolver;

        public XamlIlAstNamePropertyReference ResolvePropertyName(string pname)
        {
            if (pname.Contains("."))
            {
                var parts = pname.Split(new[] { '.' }, 2);
                var decraringType = _typeResolver(parts[0]);
                return new XamlIlAstNamePropertyReference(_lineInfo, decraringType, parts[1],
                    CurrentType.TypeReference);
            }
            else
                return new XamlIlAstNamePropertyReference(_lineInfo, CurrentType.TypeReference, pname,
                    CurrentType.TypeReference);

        }
    }
    
    class MeScannerTypeName
    {
        public XamlIlAstXmlTypeReference TypeReference { get; }

        public MeScannerTypeName(XamlIlAstXmlTypeReference typeReference)
        {
            TypeReference = typeReference;
        }
        
        public static MeScannerTypeName ParseInternal(string longName, MeScannerContext context, out string error)
        {
            error = null;
            return new MeScannerTypeName(context.TypeResolver(longName));
        }

        public string Name => TypeReference.Name;
        public string Namespace => TypeReference.XmlNamespace;
        public bool IsMarkupExtension => false;
    }

    class MeScannerSr
    {
        public static string Get(string error) => error;
        public static string Get(string error, params object[] args) => string.Format(error, args);
    }

    class MeScannerSRID
    {
        public static readonly string UnexpectedTokenAfterME = "Unexpected token after Markup Extension";
        public static readonly string MalformedBracketCharacters = "Malformed bracket characters: {0}";
        public static readonly string UnclosedQuote = "Unclosed quote";
        public static readonly string QuoteCharactersOutOfPlace = "Quote characters out of place";
        public static readonly string InvalidClosingBracketCharacers = "Invalid closing bracket characters: {0}";
        public static readonly string MalformedPropertyName = "Malformed property name";
    }

    class MeScannerParseException : Exception
    {
        public MeScannerParseException(MeScanner meScanner, string error) : base(error)
        {
            
        }

        public MeScannerParseException(string error) : base(error)
        {
            
        }
    }
    
    class MeScannerBracketModeParseParameters
    {
        public bool IsConstructorParsingMode { get; set; } = true;
        public int CurrentConstructorParam { get; set; }
        public int MaxConstructorParams { get; set; } = Int32.MaxValue;
        public bool IsBracketEscapeMode { get; set; }
        public Stack<char> BracketCharacterStack { get; set; } = new Stack<char>();
    }

    class MeScannerSpecialBracketCharacters
    {
        public bool StartsEscapeSequence(char ch) => throw new NotSupportedException();

        public bool EndsEscapeSequence(char ch) => throw new NotSupportedException();

        public bool Match(char peek, char ch) => throw new NotSupportedException();
    }
}
