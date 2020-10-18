using System;

namespace XamlParserTests
{
    public class IntrinsicsTestsClass
    {
        public object ObjectProperty { get; set; }

        public int IntProperty { get; set; }

        public Type TypeProperty { get; set; }

        public static object StaticProp { get; } = "StaticPropValue";

        public static object StaticField = "StaticFieldValue";

        public const string StringConstant = "ConstantValue";

        public const int IntConstant = 100;

        public const float FloatConstant = 2;

        public const double DoubleConstant = 3;
    }
}
