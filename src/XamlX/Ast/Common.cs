
using System.Collections.Generic;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlAstVisitor;
namespace XamlX.Ast
{
    public interface IXamlLineInfo
    {
        int Line { get; set; }
        int Position { get; set; }   
    }

    public interface IXamlAstVisitor
    {
        IXamlAstNode Visit(IXamlAstNode node);
        void Push(IXamlAstNode node);
        void Pop();
    }
    
    public interface IXamlAstNode : IXamlLineInfo
    {
        void VisitChildren(Visitor visitor);
        IXamlAstNode Visit(Visitor visitor);
    }
    
    public abstract class XamlAstNode : IXamlAstNode
    {
        public int Line { get; set; }
        public int Position { get; set; }

        public XamlAstNode(IXamlLineInfo lineInfo)
        {
            Line = lineInfo.Line;
            Position = lineInfo.Position;
        }
        
        public virtual void VisitChildren(Visitor visitor)
        {
            
        }
        
        public IXamlAstNode Visit(Visitor visitor)
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

        protected static void VisitList<T>(IList<T> list, Visitor visitor) where T : IXamlAstNode
        {
            for (var c = 0; c < list.Count; c++)
            {
                list[c] = (T) list[c].Visit(visitor);
            }
        }
    }

    public interface IXamlAstManipulationNode : IXamlAstNode
    {
        
    }

    public interface IXamlAstNodeNeedsParentStack
    {
        bool NeedsParentStack { get; }
    }

    public interface IXamlAstImperativeNode : IXamlAstNode
    {
        
    }
    
    public interface IXamlAstValueNode : IXamlAstNode
    {
        IXamlAstTypeReference Type { get; }
    }
    
    
    public interface IXamlAstTypeReference : IXamlAstNode
    {
        bool IsMarkupExtension { get; }
    }
    
    
    public interface IXamlAstPropertyReference : IXamlAstNode
    {
        
    }
    
    public static class XamlAstExtensions
    {
        public static IXamlType GetClrType(this IXamlAstTypeReference r)
        {
            if (r is XamlAstClrTypeReference clr)
                return clr.Type;
            throw new XamlParseException($"Unable to convert {r} to CLR type", r);
        }
        
        public static XamlAstClrTypeReference GetClrTypeReference(this IXamlAstTypeReference r)
        {
            if (r is XamlAstClrTypeReference clr)
                return clr;
            throw new XamlParseException($"Unable to convert {r} to CLR type", r);
        }
        
        public static XamlAstClrProperty GetClrProperty(this IXamlAstPropertyReference r)
        {
            if (r is XamlAstClrProperty clr)
                return clr;
            throw new XamlParseException($"Unable to convert {r} to CLR property", r);
        }
    }
}
