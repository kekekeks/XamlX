using System;
using System.ComponentModel;

namespace XamlParserTests
{
    public class ConvertersTestClass
    {
        [Content]
        public object ContentProperty { get; set; }

        public long Int64Property { get; set; }

        public bool BoolProperty { get; set; }

        public double DoubleProperty { get; set; }

        public float FloatProperty { get; set; }

        public TimeSpan TimeSpanProperty { get; set; }

        public Type TypeProperty { get; set; }

        public UriKind UriKindProperty { get; set; }

        public ConvertersTestValueType CustomProperty { get; set; }

        public ConvertersTestsClassWithConverter TypeWithConverterProperty { get; set; }

        [TypeConverter(typeof(PropertyTestConverter))]
        public ConvertersTestsClassWithoutConverter PropertyWithConverter { get; set; }

        public ConvertersTestsEnum EnumProperty { get; set; }
    }
}
