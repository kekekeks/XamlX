using System;
using XamlX.TypeSystem;
using Xunit;

namespace XamlParserTests
{
    public class DeferredContentTestsClass
    {
        [Content, DeferredContent]
        public object? DeferredContent { get; set; }

        public object? ObjectProperty { get; set; }
    }

    public class DeferredValue
    {
        public Func<IServiceProvider?, object> OriginalFactory { get; }

        public DeferredValue(Func<IServiceProvider?, object> originalFactory)
            => OriginalFactory = originalFactory;
    }
    
    public class DeferredContentTests : CompilerTestBase
    {
        DeferredContentTestsClass CompileAndRun(string xaml, CallbackExtensionCallback? cb)
            => (DeferredContentTestsClass)Compile(xaml).create!(new DictionaryServiceProvider
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
            var e1 = (DeferredContentTestsClass)((Func<IServiceProvider?, object>)res.DeferredContent!)(null);
            Assert.Equal("321", e1.ObjectProperty);
            var e2 = (DeferredContentTestsClass)((Func<IServiceProvider?, object>)res.DeferredContent!)(null);
            Assert.Equal("321", e2.ObjectProperty);
            Assert.NotSame(e1, e2);
        }

        class ConstantRootObjectProvider : ITestRootObjectProvider
        {
            public object? RootObject { get; set; }
        }
        
        public static Func<IServiceProvider, object> DelegateCustomizer(
            Func<IServiceProvider, object> builder,
            IServiceProvider parentServices)
        {
            var parentRoot = parentServices.GetRequiredService<ITestRootObjectProvider>().RootObject;
            var cb = parentServices.GetService<CallbackExtensionCallback>();

            return sp => builder(new DictionaryServiceProvider
            {
                [typeof(ITestRootObjectProvider)] = new ConstantRootObjectProvider {RootObject = parentRoot},
                [typeof(CallbackExtensionCallback)] = cb,
                Parent = sp
            });
        }

        public static unsafe Func<IServiceProvider, object> FunctionPointerCustomizer(
            IntPtr builder,
            IServiceProvider parentServices)
        {
            var parentRoot = parentServices.GetRequiredService<ITestRootObjectProvider>().RootObject;
            var cb = parentServices.GetService<CallbackExtensionCallback>();
            var typedBuilder = (delegate*<IServiceProvider, object>)builder;

            return sp => typedBuilder(new DictionaryServiceProvider
            {
                [typeof(ITestRootObjectProvider)] = new ConstantRootObjectProvider {RootObject = parentRoot},
                [typeof(CallbackExtensionCallback)] = cb,
                Parent = sp
            });
        }

        public static DeferredValue CustomizerWithChangedReturnType(
            Func<IServiceProvider?, object> builder,
            IServiceProvider parentServices)
            => new(builder);

        [Fact]
        public void DeferredContent_Delegate_Should_Be_Transformed_When_Configured()
        {
            Configuration.TypeMappings.DeferredContentExecutorCustomization =
                Configuration.TypeSystem.GetType(typeof(DeferredContentTests).FullName!)
                    .GetMethod(m => m.Name == nameof(DelegateCustomizer));

            var res = CompileAndRun(@"
<DeferredContentTestsClass xmlns='test'>
    <DeferredContentTestsClass ObjectProperty='{Callback}'/>
</DeferredContentTestsClass>",
                sp => sp.GetRequiredService<ITestRootObjectProvider>().RootObject);

            var generated = (DeferredContentTestsClass)((Func<IServiceProvider?, object>)res.DeferredContent!)(null);
            Assert.Same(res, generated.ObjectProperty);
        }

        [Fact]
        public void DeferredContent_Pointer_Should_Be_Transformed_When_Configured()
        {
            Configuration.TypeMappings.DeferredContentExecutorCustomization =
                Configuration.TypeSystem.GetType(typeof(DeferredContentTests).FullName!)
                    .GetMethod(m => m.Name == nameof(FunctionPointerCustomizer));

            var res = CompileAndRun(@"
<DeferredContentTestsClass xmlns='test'>
    <DeferredContentTestsClass ObjectProperty='{Callback}'/>
</DeferredContentTestsClass>",
                sp => sp.GetRequiredService<ITestRootObjectProvider>().RootObject);

            var generated = (DeferredContentTestsClass)((Func<IServiceProvider?, object>)res.DeferredContent!)(null);
            Assert.Same(res, generated.ObjectProperty);
        }

        [Fact]
        public void DeferredContent_Delegate_Should_Be_Transformed_With_Changed_Return_Type_When_Configured()
        {
            Configuration.TypeMappings.DeferredContentExecutorCustomization =
                Configuration.TypeSystem.GetType(typeof(DeferredContentTests).FullName!)
                    .GetMethod(m => m.Name == nameof(CustomizerWithChangedReturnType));

            var res = CompileAndRun(@"
<DeferredContentTestsClass xmlns='test'>
    <DeferredContentTestsClass ObjectProperty='abc'/>
</DeferredContentTestsClass>",
                sp => sp.GetRequiredService<ITestRootObjectProvider>().RootObject);

            var deferredValue = Assert.IsType<DeferredValue>(res.DeferredContent);
            var content = Assert.IsType<DeferredContentTestsClass>(deferredValue.OriginalFactory(null));
            Assert.Equal("abc", content.ObjectProperty);
        }
    }
}