
using System.Collections.Generic;
using XamlIl.TypeSystem;
using Visitor = XamlIl.Ast.XamlIlAstVisitorDelegate;
namespace XamlIl.Ast
{
    public delegate IXamlIlAstNode XamlIlAstVisitorDelegate(IXamlIlAstNode node);
    
    public interface IXamlIlLineInfo
    {
        int Line { get; set; }
        int Position { get; set; }   
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
            var node = visitor(this);
            node.VisitChildren(visitor);
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
    
    public interface IXamlIlAstValueNode : IXamlIlAstNode
    {
        IXamlIlAstTypeReference Type { get; }
    }
    
    
    public interface IXamlIlAstTypeReference : IXamlIlAstNode
    {
        
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
        
        public static IXamlIlProperty GetClrProperty(this IXamlIlAstPropertyReference r)
        {
            if (r is XamlIlAstClrPropertyReference clr)
                return clr.Property;
            throw new XamlIlParseException($"Unable to convert {r} to CLR property", r);
        }
    }
}