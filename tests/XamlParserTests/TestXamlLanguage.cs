using System;
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
}