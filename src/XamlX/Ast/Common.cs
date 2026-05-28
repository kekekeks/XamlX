using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlAstVisitor;

namespace XamlX.Ast
{
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlLineInfo
    {
        int Line { get; set; }
        int Position { get; set; }   
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstVisitor
    {
        IXamlAstNode Visit(IXamlAstNode node);
        void Push(IXamlAstNode node);
        void Pop();
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstNode : IXamlLineInfo
    {
        bool IsSkipped { get; }
        void VisitChildren(Visitor visitor);
        IXamlAstNode Visit(Visitor visitor);
    }

#if !XAMLX_INTERNAL
    public
#endif
    class SkipXamlAstNode : XamlAstNode, IXamlAstValueNode, IXamlAstManipulationNode
    {
        public SkipXamlAstNode(IXamlLineInfo lineInfo) : base(lineInfo)
        {
        }

        public override bool IsSkipped => true;

        public IXamlAstTypeReference Type => new XamlAstClrTypeReference(this, XamlPseudoType.Unknown, false);

        public override void VisitChildren(Visitor visitor) { }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class SkipXamlValueWithManipulationNode : XamlValueWithManipulationNode, IXamlAstManipulationNode
    {
        public SkipXamlValueWithManipulationNode(IXamlLineInfo lineInfo) : base(lineInfo, new SkipXamlAstNode(lineInfo), null)
        {
        }

        public override bool IsSkipped => true;

        public override void VisitChildren(Visitor visitor) { }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    abstract class XamlAstNode : IXamlAstNode
    {
        public virtual bool IsSkipped => false;
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
            if (IsSkipped)
            {
                return this;
            }

            var node = visitor.Visit(this);
            if (node is null)
            {
                throw new InvalidOperationException(
                    $"Visitor returned null IXamlAstNode from \"{GetType().Name}\" input.");
            }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void VisitList<T>(List<T> list, Visitor visitor) where T : IXamlAstNode
        {
            var count = list.Count;
            for (var c = 0; c < count; c++)
            {
                list[c] = (T) list[c].Visit(visitor);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void VisitList<T>(T[] list, Visitor visitor) where T : IXamlAstNode
        {
            var length = list.Length;
            for (var c = 0; c < length; c++)
            {
                list[c] = (T) list[c].Visit(visitor);
            }
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstManipulationNode : IXamlAstNode
    {
        
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstNodeNeedsParentStack
    {
        bool NeedsParentStack { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstImperativeNode : IXamlAstNode
    {
        
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstValueNode : IXamlAstNode
    {
        IXamlAstTypeReference Type { get; }
    }
    
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstTypeReference : IXamlAstNode
    {
        bool IsMarkupExtension { get; }
        bool Equals(IXamlAstTypeReference other);
    }
    
    
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstPropertyReference : IXamlAstNode
    {
        
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    static class XamlAstExtensions
    {
        public static IXamlType GetClrType(this IXamlAstTypeReference r)
        {
            if (r is XamlAstClrTypeReference clr)
                return clr.Type;
            throw new XamlTransformException($"Unable to convert {r} to CLR type", r);
        }
        
        public static XamlAstClrTypeReference GetClrTypeReference(this IXamlAstTypeReference r)
        {
            if (r is XamlAstClrTypeReference clr)
                return clr;
            throw new XamlTransformException($"Unable to convert {r} to CLR type", r);
        }
        
        public static XamlAstClrProperty GetClrProperty(this IXamlAstPropertyReference r)
        {
            if (r is XamlAstClrProperty clr)
                return clr;
            throw new XamlTransformException($"Unable to convert {r} to CLR property", r);
        }
    }
}
