using System;

namespace XamlParserTests
{
    public class CustomizerClass
    {
        public static Func<IServiceProvider, object> Customizer(Func<IServiceProvider, object> builder,
            IServiceProvider parentServices)
        {
            var parentRoot = parentServices.GetService<ITestRootObjectProvider>().RootObject;
            var cb = parentServices.GetService<CallbackExtensionCallback>();

            return sp => builder(new DictionaryServiceProvider
            {
                [typeof(ITestRootObjectProvider)] = new ConstantRootObjectProvider { RootObject = parentRoot },
                [typeof(CallbackExtensionCallback)] = cb,
                Parent = sp
            });
        }
    }
}
