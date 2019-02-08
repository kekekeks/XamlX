using System;
using System.Collections.Generic;
using XamlParserTests;

[assembly: XmlnsDefinition("test", "XamlParserTests")]
namespace XamlParserTests
{
    public class ContentAttribute : Attribute
    {
        
    }

    public class XmlnsDefinitionAttribute : Attribute
    {
        public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
        {
            
        }
    }

    public class UsableDuringInitializationAttribute : Attribute
    {
        public UsableDuringInitializationAttribute(bool usable)
        {
            
        }
    }
    
    public interface ITestRootObjectProvider
    {
        object RootObject { get; }
    }

    public interface IXamlParentStack
    {
        IEnumerable<object> Parents { get; }
    }
}