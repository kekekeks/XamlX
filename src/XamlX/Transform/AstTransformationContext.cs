using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private IXamlAstValueNode? _rootObject;

        public virtual string? Document { get; }
        public Dictionary<string, string> NamespaceAliases { get; set; }
        public TransformerConfiguration Configuration { get; }

        public IXamlAstValueNode RootObject
        {
            get => _rootObject ?? throw new InvalidOperationException($"{nameof(RootObject)} hasn't been set");
            set => _rootObject = value;
        }

        public AstTransformationContext(
            TransformerConfiguration configuration,
            XamlDocument? xamlDocument)
        {
            Configuration = configuration;
            NamespaceAliases = xamlDocument?.NamespaceAliases ?? new();
            Document = xamlDocument?.Document;
        }

#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
        public XamlDiagnosticSeverity ReportDiagnostic(XamlDiagnostic diagnostic, bool throwOnFatal = true)
        {
            if (string.IsNullOrWhiteSpace(diagnostic.Document))
            {
                diagnostic = diagnostic with { Document = Document };
            }
            
            var severity = Configuration.DiagnosticsHandler.ReportDiagnostic(diagnostic);
            if (throwOnFatal && severity >= XamlDiagnosticSeverity.Fatal)
            {
                throw diagnostic.ToException();
            }
            return severity;
        }

        protected abstract class ContextXamlAstVisitor : IXamlAstVisitor
        {
            private readonly AstTransformationContext _context;

            public ContextXamlAstVisitor(AstTransformationContext context)
            {
                _context = context;
            }

            public abstract string GetTransformerInfo();
            public abstract IXamlAstNode VisitCore(AstTransformationContext context, IXamlAstNode node); 
            
            public IXamlAstNode Visit(IXamlAstNode node)
            {
                try
                {
                    var outputNode = VisitCore(_context, node);
                    if (outputNode is null)
                    {
                        throw new InvalidOperationException(
                            $"\"{GetTransformerInfo()}\" returned null IXamlAstNode.");
                    }
                    return outputNode;
                }
                catch (Exception e)
                {
                    var reportException = e is XmlException
                        ? e
                        : new XamlTransformException(
                            $"Internal compiler error: {e.Message} ({GetTransformerInfo()})",
                            node, innerException: e)
                        {
                            Document = _context.Document
                        };
                    
                    if (_context.OnUnhandledTransformError(reportException))
                    {
                        return node is IXamlAstTypeReference
                            ? new XamlAstClrTypeReference(node, XamlPseudoType.Unknown, false)
                            : new SkipXamlValueWithManipulationNode(node);
                    }
                    else
                    {
#if DEBUG
                        throw;
#else 
                        throw reportException;
#endif
                    }
                }
            }

            public void Push(IXamlAstNode node) => _context.PushParent(node);

            public void Pop() => _context.PopParent();
        }
        
        class Visitor : ContextXamlAstVisitor
        {
            private readonly IXamlAstTransformer _transformer;

            public Visitor(AstTransformationContext context, IXamlAstTransformer transformer) : base(context)
            {
                _transformer = transformer;
            }

            public override string GetTransformerInfo() => _transformer.GetType().Name;

            public override IXamlAstNode VisitCore(AstTransformationContext context, IXamlAstNode node) =>
                _transformer.Transform(context, node);
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

        protected bool OnUnhandledTransformError(Exception exception)
        {
            var severity = ReportDiagnostic(exception.ToDiagnostic(this), false);
            return severity < XamlDiagnosticSeverity.Fatal;
        }
    }
}
