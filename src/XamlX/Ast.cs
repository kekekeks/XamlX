using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace XamlX
{
    public interface IXamlXAstNode
    {
        int Line { get; set; }
        int Position { get; set; }
    }
    
    public abstract class XamlXAstNode : IXamlXAstNode
    {
        public int Line { get; set; }
        public int Position { get; set; }
    }

    public interface IXamlXAstManipulationNode : IXamlXAstNode
    {
        
    }
    
    public interface IXamlXAstValueNode : IXamlXAstManipulationNode
    {
        
    }

    public class XamlXAstTextNode : XamlXAstNode, IXamlXAstValueNode
    {
        public string Text { get; set; }

        public XamlXAstTextNode(string text)
        {
            Text = text;
        }
    }

    public class XamlXAstValueNodeList : XamlXAstNode, IXamlXAstValueNode
    {
        public List<IXamlXAstValueNode> Children { get; set; } = new List<IXamlXAstValueNode>();
    }
    
    public class XamlXAstNewInstanceNode : XamlXAstNode, IXamlXAstValueNode
    {
        public XamlXAstNewInstanceNode(IXamlXAstTypeReference type)
        {
            Type = type;
        }

        public IXamlXAstTypeReference Type { get; set; }
        public List<IXamlXAstManipulationNode> Children { get; set; } = new List<IXamlXAstManipulationNode>();
    }

    public class XamlXAstRootInstanceNode : XamlXAstNewInstanceNode
    {
        public Dictionary<string, string> XmlNamespaces { get; set; } = new Dictionary<string, string>();

        public XamlXAstRootInstanceNode(IXamlXAstTypeReference type) : base(type)
        {
        }
    }

    public class XamlXAstPropertyAssignmentNode : XamlXAstNode, IXamlXAstManipulationNode
    {
        public IXamlXAstPropertyReference Property { get; set; }
        public IXamlXAstValueNode Value { get; set; }

        public XamlXAstPropertyAssignmentNode(IXamlXAstPropertyReference property, IXamlXAstValueNode value)
        {
            Property = property;
            Value = value;
        }
    }

    public interface IXamlXAstTypeReference : IXamlXAstNode
    {
        
    }

    public class XamlXAstXmlTypeReference : XamlXAstNode, IXamlXAstTypeReference
    {
        public string XmlNamespace { get; set; }
        public string Name { get; set; }

        public XamlXAstXmlTypeReference(string xmlNamespace, string name)
        {
            XmlNamespace = xmlNamespace;
            Name = name;
        }
    }

    public class XamlXAstClrNameTypeReference : XamlXAstNode, IXamlXAstTypeReference
    {
        public string Namespace { get; set; }
        public string Name { get; set; }

    }

    public interface IXamlXAstPropertyReference : IXamlXAstNode
    {
        
    }

    public class XamlXAstNamePropertyReference : XamlXAstNode, IXamlXAstPropertyReference
    {
        public IXamlXAstTypeReference Type { get; set; }
        public string Name { get; set; }

        public XamlXAstNamePropertyReference(IXamlXAstTypeReference type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public interface IXamlXAstDirective : IXamlXAstManipulationNode
    {
        
    }
    
    public class XamlXAstXmlDirective : XamlXAstNode, IXamlXAstDirective
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public IXamlXAstValueNode Value { get; set; }

        public XamlXAstXmlDirective(string ns, string name, IXamlXAstValueNode value)
        {
            Namespace = ns;
            Name = name;
            Value = value;
        }
        
    }
    
    public class XamlXAstMarkupExtensionNode : XamlXAstNode, IXamlXAstValueNode
    {
        public IXamlXAstTypeReference Type { get; set; }
        public List<IXamlXAstNode> ConstructorArguments { get; set; } = new List<IXamlXAstNode>();

        public List<XamlXAstPropertyAssignmentNode> Properties { get; set; } =
            new List<XamlXAstPropertyAssignmentNode>();
    }

}