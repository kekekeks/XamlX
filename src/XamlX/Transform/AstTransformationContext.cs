using System;
using System.Collections.Generic;
using System.Xml;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class AstTransformationContext : XamlContextBase
    {
        public Dictionary<string, string> NamespaceAliases { get; set; } = new Dictionary<string, string>();      
        public TransformerConfiguration Configuration { get; }
        public IXamlAstValueNode RootObject { get; set; }
        public bool StrictMode { get; }

        public IXamlAstNode Error(IXamlAstNode node, Exception e)
        {
            if (StrictMode)
                throw e;
            return node;
        }

        public IXamlAstNode ParseError(string message, IXamlAstNode node) =>
            Error(node, new XamlParseException(message, node));
        
        public IXamlAstNode ParseError(string message, IXamlAstNode offender, IXamlAstNode ret) =>
            Error(ret, new XamlParseException(message, offender));

        public AstTransformationContext(TransformerConfiguration configuration,
            Dictionary<string, string> namespaceAliases, bool strictMode = true)
        {
            Configuration = configuration;
            NamespaceAliases = namespaceAliases;
            StrictMode = strictMode;
        }

        class Visitor : IXamlAstVisitor
        {
            private readonly AstTransformationContext _context;
            private readonly IXamlAstTransformer _transformer;

            public Visitor(AstTransformationContext context, IXamlAstTransformer transformer)
            {
                _context = context;
                _transformer = transformer;
            }
            
            public IXamlAstNode Visit(IXamlAstNode node)
            {
                #if Xaml_DEBUG
                return _transformer.Transform(_context, node);
                #else
                try
                {
                    return _transformer.Transform(_context, node);
                }
                catch (Exception e) when (!(e is XmlException))
                {
                    throw new XamlParseException(
                        "Internal compiler error while transforming node " + node + ":\n" + e, node);
                }
                #endif
            }

            public void Push(IXamlAstNode node) => _context.PushParent(node);

            public void Pop() => _context.PopParent();
        }
        
        public IXamlAstNode Visit(IXamlAstNode root, IXamlAstTransformer transformer)
        {
            root = root.Visit(new Visitor(this, transformer));
            return root;
        }

        public void VisitChildren(IXamlAstNode root, IXamlAstTransformer transformer)
        {
            root.VisitChildren(new Visitor(this, transformer));
        }
    }
}
