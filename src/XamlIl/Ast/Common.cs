
using System.Collections.Generic;
using XamlIl.TypeSystem;
using Visitor = XamlIl.Ast.IXamlIlAstVisitor;
namespace XamlIl.Ast
{
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlLineInfo
    {
        int Line { get; set; }
        int Position { get; set; }   
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlAstVisitor
    {
        IXamlIlAstNode Visit(IXamlIlAstNode node);
        void Push(IXamlIlAstNode node);
        void Pop();
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlAstNode : IXamlIlLineInfo
    {
        void VisitChildren(Visitor visitor);
        IXamlIlAstNode Visit(Visitor visitor);
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    abstract class XamlIlAstNode : IXamlIlAstNode
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

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlAstManipulationNode : IXamlIlAstNode
    {
        
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlAstNodeNeedsParentStack
    {
        bool NeedsParentStack { get; }
    }

#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlAstImperativeNode : IXamlIlAstNode
    {
        
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlAstValueNode : IXamlIlAstNode
    {
        IXamlIlAstTypeReference Type { get; }
    }
    
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlAstTypeReference : IXamlIlAstNode
    {
        bool IsMarkupExtension { get; }
        bool Equals(IXamlIlAstTypeReference other);
    }
    
    
#if !XAMLIL_INTERNAL
    public
#endif
    interface IXamlIlAstPropertyReference : IXamlIlAstNode
    {
        
    }
    
#if !XAMLIL_INTERNAL
    public
#endif
    static class XamlIlAstExtensions
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
        
        public static XamlIlAstClrProperty GetClrProperty(this IXamlIlAstPropertyReference r)
        {
            if (r is XamlIlAstClrProperty clr)
                return clr;
            throw new XamlIlParseException($"Unable to convert {r} to CLR property", r);
        }
    }
}
