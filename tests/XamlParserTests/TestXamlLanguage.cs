using System;
using System.Collections.Generic;
using XamlParserTests;

[assembly: XmlnsDefinition("test", "XamlParserTests")]
namespace XamlParserTests
{
    public interface IAddChild
    {
        void AddChild(object child);
    }

    public interface IAddChild<T> : IAddChild
    {
        void AddChild(T child);
    }

    public class ContentAttribute : Attribute
    {
        
    }

    public class WhitespaceSignificantCollectionAttribute : Attribute
    {

    }

    public class TrimSurroundingWhitespaceAttribute : Attribute
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
        object? RootObject { get; }
    }

    public interface ITestProvideValueTarget
    {
        object? TargetObject { get; }
        object? TargetProperty { get; }
    }
    
    public interface ITestUriContext
    {
        Uri? BaseUri { get; set; }
    }
    
}