
using System.Collections.Generic;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlXAstVisitor;
namespace XamlX.Ast
{
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXLineInfo
    {
        int Line { get; set; }
        int Position { get; set; }   
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXAstVisitor
    {
        IXamlXAstNode Visit(IXamlXAstNode node);
        void Push(IXamlXAstNode node);
        void Pop();
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXAstNode : IXamlXLineInfo
    {
        void VisitChildren(Visitor visitor);
        IXamlXAstNode Visit(Visitor visitor);
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    abstract class XamlXAstNode : IXamlXAstNode
    {
        public int Line { get; set; }
        public int Position { get; set; }

        public XamlXAstNode(IXamlXLineInfo lineInfo)
        {
            Line = lineInfo.Line;
            Position = lineInfo.Position;
        }
        
        public virtual void VisitChildren(Visitor visitor)
        {
            
        }
        
        public IXamlXAstNode Visit(Visitor visitor)
        {
            var node = visitor.Visit(this);
            try
            {
                visitor.Push(node);
                node.VisitChildren(visitor);
            }
            finally
            {
                visitor.Pop();
            }

            return node;
        }

        protected static void VisitList<T>(IList<T> list, Visitor visitor) where T : IXamlXAstNode
        {
            for (var c = 0; c < list.Count; c++)
            {
                list[c] = (T) list[c].Visit(visitor);
            }
        }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXAstManipulationNode : IXamlXAstNode
    {
        
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXAstNodeNeedsParentStack
    {
        bool NeedsParentStack { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXAstImperativeNode : IXamlXAstNode
    {
        
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXAstValueNode : IXamlXAstNode
    {
        IXamlXAstTypeReference Type { get; }
    }
    
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXAstTypeReference : IXamlXAstNode
    {
        bool IsMarkupExtension { get; }
        bool Equals(IXamlXAstTypeReference other);
    }
    
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlXAstPropertyReference : IXamlXAstNode
    {
        
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    static class XamlXAstExtensions
    {
        public static IXamlXType GetClrType(this IXamlXAstTypeReference r)
        {
            if (r is XamlXAstClrTypeReference clr)
                return clr.Type;
            throw new XamlXParseException($"Unable to convert {r} to CLR type", r);
        }
        
        public static XamlXAstClrTypeReference GetClrTypeReference(this IXamlXAstTypeReference r)
        {
            if (r is XamlXAstClrTypeReference clr)
                return clr;
            throw new XamlXParseException($"Unable to convert {r} to CLR type", r);
        }
        
        public static XamlXAstClrProperty GetClrProperty(this IXamlXAstPropertyReference r)
        {
            if (r is XamlXAstClrProperty clr)
                return clr;
            throw new XamlXParseException($"Unable to convert {r} to CLR property", r);
        }
    }
}
