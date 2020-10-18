using System;
using XamlX.TypeSystem;
using Xunit;

namespace XamlParserTests
{
    public class DeferredContentTests : CompilerTestBase
    {
        private DeferredContentTestsClass CompileAndRun(string xaml, CallbackExtensionCallback cb)
            => (DeferredContentTestsClass)Compile(xaml).create(new DictionaryServiceProvider
            {
                [typeof(CallbackExtensionCallback)] = cb,
            });

        [Fact]
        public void DeferredContent_Should_Generate_Delegate_In_The_Target_Property()
        {
            var res = CompileAndRun(@"
<DeferredContentTestsClass xmlns='test' ObjectProperty='123'>
    <DeferredContentTestsClass ObjectProperty='321'/>
</DeferredContentTestsClass>", null);

            Assert.Equal("123", res.ObjectProperty);
            var e1 = (DeferredContentTestsClass)((Func<IServiceProvider, object>)res.DeferredContent)(null);
            Assert.Equal("321", e1.ObjectProperty);
            var e2 = (DeferredContentTestsClass)((Func<IServiceProvider, object>)res.DeferredContent)(null);
            Assert.Equal("321", e2.ObjectProperty);
            Assert.NotSame(e1, e2);
        }
        
        [Fact]
        public void DeferredContent_Delegate_Should_Be_Transformed_When_Configured()
        {
            Configuration.TypeMappings.DeferredContentExecutorCustomization =
                Configuration.TypeSystem.FindType(typeof(CustomizerClass).FullName)
                    .FindMethod(m => m.Name == "Customizer");


            var res = CompileAndRun(@"
<DeferredContentTestsClass xmlns='test'>
    <DeferredContentTestsClass ObjectProperty='{Callback}'/>
</DeferredContentTestsClass>",
                sp => sp.GetService<ITestRootObjectProvider>().RootObject);

            var generated = (DeferredContentTestsClass)((Func<IServiceProvider, object>)res.DeferredContent)(null);
            Assert.Same(res, generated.ObjectProperty);

        }
    }
}