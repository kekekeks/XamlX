using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace XamlIl
{
    public interface IXamlIlAstNode
    {
        int Line { get; set; }
        int Position { get; set; }
    }
    
    public abstract class XamlIlAstNode : IXamlIlAstNode
    {
        public int Line { get; set; }
        public int Position { get; set; }
    }

    public interface IXamlIlAstManipulationNode : IXamlIlAstNode
    {
        
    }
    
    public interface IXamlIlAstValueNode : IXamlIlAstManipulationNode
    {
        
    }

    public class XamlIlAstTextNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public string Text { get; set; }

        public XamlIlAstTextNode(string text)
        {
            Text = text;
        }
    }

    public class XamlIlAstValueNodeList : XamlIlAstNode, IXamlIlAstValueNode
    {
        public List<IXamlIlAstValueNode> Children { get; set; } = new List<IXamlIlAstValueNode>();
    }
    
    public class XamlIlAstNewInstanceNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public XamlIlAstNewInstanceNode(IXamlIlAstTypeReference type)
        {
            Type = type;
        }

        public IXamlIlAstTypeReference Type { get; set; }
        public List<IXamlIlAstManipulationNode> Children { get; set; } = new List<IXamlIlAstManipulationNode>();
    }

    public class XamlIlAstRootInstanceNode : XamlIlAstNewInstanceNode
    {
        public Dictionary<string, string> XmlNamespaces { get; set; } = new Dictionary<string, string>();

        public XamlIlAstRootInstanceNode(IXamlIlAstTypeReference type) : base(type)
        {
        }
    }

    public class XamlIlAstPropertyAssignmentNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public IXamlIlAstPropertyReference Property { get; set; }
        public IXamlIlAstValueNode Value { get; set; }

        public XamlIlAstPropertyAssignmentNode(IXamlIlAstPropertyReference property, IXamlIlAstValueNode value)
        {
            Property = property;
            Value = value;
        }
    }

    public interface IXamlIlAstTypeReference : IXamlIlAstNode
    {
        
    }

    public class XamlIlAstXmlTypeReference : XamlIlAstNode, IXamlIlAstTypeReference
    {
        public string XmlNamespace { get; set; }
        public string Name { get; set; }

        public XamlIlAstXmlTypeReference(string xmlNamespace, string name)
        {
            XmlNamespace = xmlNamespace;
            Name = name;
        }
    }

    public class XamlIlAstClrNameTypeReference : XamlIlAstNode, IXamlIlAstTypeReference
    {
        public string Namespace { get; set; }
        public string Name { get; set; }

    }

    public interface IXamlIlAstPropertyReference : IXamlIlAstNode
    {
        
    }

    public class XamlIlAstNamePropertyReference : XamlIlAstNode, IXamlIlAstPropertyReference
    {
        public IXamlIlAstTypeReference Type { get; set; }
        public string Name { get; set; }

        public XamlIlAstNamePropertyReference(IXamlIlAstTypeReference type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public interface IXamlIlAstDirective : IXamlIlAstManipulationNode
    {
        
    }
    
    public class XamlIlAstXmlDirective : XamlIlAstNode, IXamlIlAstDirective
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public IXamlIlAstValueNode Value { get; set; }

        public XamlIlAstXmlDirective(string ns, string name, IXamlIlAstValueNode value)
        {
            Namespace = ns;
            Name = name;
            Value = value;
        }
        
    }
    
    public class XamlIlAstMarkupExtensionNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public IXamlIlAstTypeReference Type { get; set; }
        public List<IXamlIlAstNode> ConstructorArguments { get; set; } = new List<IXamlIlAstNode>();

        public List<XamlIlAstPropertyAssignmentNode> Properties { get; set; } =
            new List<XamlIlAstPropertyAssignmentNode>();
    }

}