using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.TypeSystem;
using Visitor = XamlX.Ast.IXamlAstVisitor;
namespace XamlX.Ast
{

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstXmlDirective : XamlAstNode, IXamlAstManipulationNode
    {
        public string? Namespace { get; set; }
        public string Name { get; set; }
        public List<IXamlAstValueNode> Values { get; set; }

        public XamlAstXmlDirective(IXamlLineInfo lineInfo,
            string? ns, string name, IEnumerable<IXamlAstValueNode> values) : base(lineInfo)
        {
            Namespace = ns;
            Name = name;
            Values = values.ToList();
        }

        public override void VisitChildren(Visitor visitor)
        {
            VisitList(Values, visitor);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstXamlPropertyValueNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlAstPropertyReference Property { get; set; }
        public List<IXamlAstValueNode> Values { get; set; }
        public bool IsAttributeSyntax { get; }

        public XamlAstXamlPropertyValueNode(IXamlLineInfo lineInfo,
            IXamlAstPropertyReference property, IXamlAstValueNode value, bool isAttributeSyntax) : base(lineInfo)
        {
            Property = property;
            Values = new List<IXamlAstValueNode> {value};
            IsAttributeSyntax = isAttributeSyntax;
        }
        
        public XamlAstXamlPropertyValueNode(IXamlLineInfo lineInfo,
            IXamlAstPropertyReference property, IEnumerable<IXamlAstValueNode> values, bool isAttributeSyntax) : base(lineInfo)
        {
            Property = property;
            Values = values.ToList();
            IsAttributeSyntax = isAttributeSyntax;
        }

        public override void VisitChildren(Visitor visitor)
        {
            Property = (IXamlAstPropertyReference) Property.Visit(visitor);
            VisitList(Values, visitor);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstObjectNode : XamlAstNode, IXamlAstValueNode
    {
        private IXamlAstTypeReference _type;

        public XamlAstObjectNode(IXamlLineInfo lineInfo, IXamlAstTypeReference type) : base(lineInfo)
        {
            _type = type;
        }

        public IXamlAstTypeReference Type
        {
            get => _type;
            set => _type = value ?? throw new InvalidOperationException("XamlAstObjectNode.Type cannot be null.");
        }
        public List<IXamlAstNode> Children { get; set; } = new List<IXamlAstNode>();
        public List<IXamlAstValueNode> Arguments { get; set; } = new List<IXamlAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
            VisitList(Children, visitor);
        }
    }
    
    

#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstTextNode : XamlAstNode, IXamlAstValueNode
    {
        public string Text { get; set; }

        /// <summary>
        /// Indicates whether this value was created from an XML node where xml:space="preserve" was in effect.
        /// </summary>
        public bool PreserveWhitespace { get; }


        /// <summary>
        /// Initializes a new instance of <see cref="XamlAstTextNode"/>.
        /// </summary>
        /// <param name="lineInfo">The line information for the node.</param>
        /// <param name="text">The node text.</param>
        /// <param name="preserveWhitespace">True if XAML whitespace normalization should NOT be applied to this text value (i.e. xml:space="preserve" or attribute values).</param>
        /// <param name="type">The type of the node.</param>
        public XamlAstTextNode(IXamlLineInfo lineInfo, string text, bool preserveWhitespace = false, IXamlType? type = null) : base(lineInfo)
        {
            Text = text;
            PreserveWhitespace = preserveWhitespace;
            if (type != null)
                Type = new XamlAstClrTypeReference(lineInfo, type, false);
            else
                Type = new XamlAstXmlTypeReference(lineInfo, XamlNamespaces.Xaml2006, "String");
        }

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlAstTypeReference) Type.Visit(visitor);
        }

        public IXamlAstTypeReference Type { get; set; }
    }
    
#if !XAMLX_INTERNAL
    public
#endif
    class XamlAstNamePropertyReference : XamlAstNode, IXamlAstPropertyReference
    {
        public IXamlAstTypeReference DeclaringType { get; set; }
        public string Name { get; set; }
        public IXamlAstTypeReference TargetType { get; set; }

        public XamlAstNamePropertyReference(IXamlLineInfo lineInfo,
            IXamlAstTypeReference declaringType, string name, IXamlAstTypeReference targetType) : base(lineInfo)
        {
            DeclaringType = declaringType;
            Name = name;
            TargetType = targetType;
        }

        public override void VisitChildren(Visitor visitor)
        {
            DeclaringType = (IXamlAstTypeReference) DeclaringType.Visit(visitor);
            TargetType = (IXamlAstTypeReference) TargetType.Visit(visitor);
        }
    }
}
