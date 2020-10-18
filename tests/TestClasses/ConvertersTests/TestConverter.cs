using System.ComponentModel;
using System.Globalization;
using Xunit;

namespace XamlParserTests
{
    public class TestConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            Assert.Equal(CultureInfo.InvariantCulture, culture);
            Assert.NotNull(context.GetService<ITestRootObjectProvider>().RootObject);
            return new ConvertersTestsClassWithConverter { Value = (string)value };
        }
    }
}
