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

    public class DeferredContentAttribute : Attribute
    {
        
    }
    
    public interface ITestRootObjectProvider
    {
        object RootObject { get; }
    }

    public interface ITestProvideValueTarget
    {
        object TargetObject { get; }
        object TargetProperty { get; }
    }
    
    public interface ITestUriContext
    {
        Uri BaseUri { get; set; }
    }
    
}