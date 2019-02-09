
using System.Collections.Generic;

[assembly: Portable.Xaml.Markup.XmlnsDefinition ("http://example.com/benchmark", "Benchmarks")]

//[assembly: System.Windows.Markup.XmlnsDefinition ("http://example.com/benchmark", "Benchmarks")]

namespace Benchmarks
{
    [Portable.Xaml.Markup.ContentProperty("Children")]
//    [System.Windows.Markup.ContentProperty("Children")]
    public class TestObject
    {
        public string StringProperty { get; set; }
        [Content]
        public List<ChildObject> Children { get; } = new List<ChildObject>();
    }

    public class ChildObject
    {
        public string StringProperty { get; set; }
        public bool BoolProperty { get; set; }
        public double DoubleProperty { get; set; }
        public int IntProperty { get; set; }
    }

}