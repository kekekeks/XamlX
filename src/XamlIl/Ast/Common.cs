
using System.Collections.Generic;
using XamlIl.TypeSystem;
using Visitor = XamlIl.Ast.IXamlIlAstVisitor;
namespace XamlIl.Ast
{
    public interface IXamlIlLineInfo
    {
        int Line { get; set; }
        int Position { get; set; }   
    }

    public interface IXamlIlAstVisitor
    {
        IXamlIlAstNode Visit(IXamlIlAstNode node);
        void Push(IXamlIlAstNode node);
        void Pop();
    }
    
    public interface IXamlIlAstNode : IXamlIlLineInfo
    {
        void VisitChildren(Visitor visitor);
        IXamlIlAstNode Visit(Visitor visitor);
    }
    
    public abstract class XamlIlAstNode : IXamlIlAstNode
    {
        public int Line { get; set; }
        public int Position { get; set; }

        public XamlIlAstNode(IXamlIlLineInfo lineInfo)
        {
            Line = lineInfo.Line;
            Position = lineInfo.Position;
        }
        
        public virtual void VisitChildren(Visitor visitor)
        {
            
        }
        
        public IXamlIlAstNode Visit(Visitor visitor)
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

        protected static void VisitList<T>(IList<T> list, Visitor visitor) where T : IXamlIlAstNode
        {
            for (var c = 0; c < list.Count; c++)
            {
                list[c] = (T) list[c].Visit(visitor);
            }
        }
    }

    public interface IXamlIlAstManipulationNode : IXamlIlAstNode
    {
        
    }

    public interface IXamlIlAstNodeNeedsParentStack
    {
        bool NeedsParentStack { get; }
    }

    public interface IXamlIlAstImperativeNode : IXamlIlAstNode
    {
        
    }
    
    public interface IXamlIlAstValueNode : IXamlIlAstNode
    {
        IXamlIlAstTypeReference Type { get; }
    }
    
    
    public interface IXamlIlAstTypeReference : IXamlIlAstNode
    {
        bool IsMarkupExtension { get; }
    }
    
    
    public interface IXamlIlAstPropertyReference : IXamlIlAstNode
    {
        
    }
    
    public static class XamlIlAstExtensions
    {
        public static IXamlIlType GetClrType(this IXamlIlAstTypeReference r)
        {
            if (r is XamlIlAstClrTypeReference clr)
                return clr.Type;
            throw new XamlIlParseException($"Unable to convert {r} to CLR type", r);
        }
        
        public static XamlIlAstClrTypeReference GetClrTypeReference(this IXamlIlAstTypeReference r)
        {
            if (r is XamlIlAstClrTypeReference clr)
                return clr;
            throw new XamlIlParseException($"Unable to convert {r} to CLR type", r);
        }
        
        public static IXamlIlProperty GetClrProperty(this IXamlIlAstPropertyReference r)
        {
            if (r is XamlIlAstClrPropertyReference clr)
                return clr.Property;
            throw new XamlIlParseException($"Unable to convert {r} to CLR property", r);
        }
    }
}
