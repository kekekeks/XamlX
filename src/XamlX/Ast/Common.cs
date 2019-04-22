
using System.Collections.Generic;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlXAstVisitor;
namespace XamlX.Ast
{
    public interface IXamlXLineInfo
    {
        int Line { get; set; }
        int Position { get; set; }   
    }

    public interface IXamlXAstVisitor
    {
        IXamlXAstNode Visit(IXamlXAstNode node);
        void Push(IXamlXAstNode node);
        void Pop();
    }
    
    public interface IXamlXAstNode : IXamlXLineInfo
    {
        void VisitChildren(Visitor visitor);
        IXamlXAstNode Visit(Visitor visitor);
    }
    
    public abstract class XamlXAstNode : IXamlXAstNode
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

    public interface IXamlXAstManipulationNode : IXamlXAstNode
    {
        
    }

    public interface IXamlXAstNodeNeedsParentStack
    {
        bool NeedsParentStack { get; }
    }

    public interface IXamlXAstImperativeNode : IXamlXAstNode
    {
        
    }
    
    public interface IXamlXAstValueNode : IXamlXAstNode
    {
        IXamlXAstTypeReference Type { get; }
    }
    
    
    public interface IXamlXAstTypeReference : IXamlXAstNode
    {
        bool IsMarkupExtension { get; }
    }
    
    
    public interface IXamlXAstPropertyReference : IXamlXAstNode
    {
        
    }
    
    public static class XamlXAstExtensions
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
        
        public static IXamlXProperty GetClrProperty(this IXamlXAstPropertyReference r)
        {
            if (r is XamlXAstClrPropertyReference clr)
                return clr.Property;
            throw new XamlXParseException($"Unable to convert {r} to CLR property", r);
        }
    }
}
