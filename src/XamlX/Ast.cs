using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace XamlX
{
    public interface IXamlAstNode
    {
        int Line { get; set; }
        int Position { get; set; }
    }
    
    public abstract class XamlAstNode : IXamlAstNode
    {
        public int Line { get; set; }
        public int Position { get; set; }
    }

    public interface IXamlAstManipulationNode : IXamlAstNode
    {
        
    }
    
    public interface IXamlAstValueNode : IXamlAstManipulationNode
    {
        
    }

    public class XamlAstTextNode : XamlAstNode, IXamlAstValueNode
    {
        public string Text { get; set; }

        public XamlAstTextNode(string text)
        {
            Text = text;
        }
    }

    public class XamlXAstValueNodeList : XamlAstNode, IXamlAstValueNode
    {
        public List<IXamlAstValueNode> Children { get; set; } = new List<IXamlAstValueNode>();
    }
    
    public class XamlXAstNewInstanceNode : XamlAstNode, IXamlAstValueNode
    {
        public XamlXAstNewInstanceNode(IXamlAstTypeReference type)
        {
            Type = type;
        }

        public IXamlAstTypeReference Type { get; set; }
        public List<IXamlAstManipulationNode> Children { get; set; } = new List<IXamlAstManipulationNode>();
    }

    public class XamlXAstRootInstanceNode : XamlXAstNewInstanceNode
    {
        public Dictionary<string, string> XmlNamespaces { get; set; } = new Dictionary<string, string>();

        public XamlXAstRootInstanceNode(IXamlAstTypeReference type) : base(type)
        {
        }
    }

    public class XamlXAstPropertyAssignmentNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlAstPropertyReference Property { get; set; }
        public IXamlAstValueNode Value { get; set; }

        public XamlXAstPropertyAssignmentNode(IXamlAstPropertyReference property, IXamlAstValueNode value)
        {
            Property = property;
            Value = value;
        }
    }

    public interface IXamlAstTypeReference : IXamlAstNode
    {
        
    }

    public class XamlAstXmlTypeReference : XamlAstNode, IXamlAstTypeReference
    {
        public string XmlNamespace { get; set; }
        public string Name { get; set; }

        public XamlAstXmlTypeReference(string xmlNamespace, string name)
        {
            XmlNamespace = xmlNamespace;
            Name = name;
        }
    }

    public class XamlXAstClrNameTypeReference : XamlAstNode, IXamlAstTypeReference
    {
        public string Namespace { get; set; }
        public string Name { get; set; }

    }

    public interface IXamlAstPropertyReference : IXamlAstNode
    {
        
    }

    public class XamlAstNamePropertyReference : XamlAstNode, IXamlAstPropertyReference
    {
        public IXamlAstTypeReference Type { get; set; }
        public string Name { get; set; }

        public XamlAstNamePropertyReference(IXamlAstTypeReference type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public interface IXamlXAstDirective : IXamlAstManipulationNode
    {
        
    }
    
    public class XamlAstXmlDirective : XamlAstNode, IXamlXAstDirective
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public IXamlAstValueNode Value { get; set; }

        public XamlAstXmlDirective(string ns, string name, IXamlAstValueNode value)
        {
            Namespace = ns;
            Name = name;
            Value = value;
        }
        
    }
    
    public class XamlXAstMarkupExtensionNode : XamlAstNode, IXamlAstValueNode
    {
        public IXamlAstTypeReference Type { get; set; }
        public List<IXamlAstNode> ConstructorArguments { get; set; } = new List<IXamlAstNode>();

        public List<XamlXAstPropertyAssignmentNode> Properties { get; set; } =
            new List<XamlXAstPropertyAssignmentNode>();
    }

}