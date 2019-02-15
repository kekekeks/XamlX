using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XamlX.Runtime;
using XamlX.TypeSystem;
using Xunit;

namespace XamlParserTests
{

    public class CallbackExtension
    {
        public object ProvideValue(IServiceProvider provider)
        {
            return provider.GetService<CallbackExtensionCallback>()(provider);
        }
    }

    public delegate object CallbackExtensionCallback(IServiceProvider provider);

    public class UnknownServiceUsageExtension
    {
        public object Return { get; set; }
        public object ProvideValue(IServiceProvider provider)
        {
            Assert.Null(provider.GetService(typeof(string)));
            return Return;
        }
    }
    
    public class ServiceProviderTestsClass
    {
        public string Id { get; set; }
        public object Property { get; set; }
        public ServiceProviderTestsClass Child { get; set; }
        [Content]
        public List<ServiceProviderTestsClass> Children { get; } = new List<ServiceProviderTestsClass>(); 
    }
    
    public class ServiceProviderTests : CompilerTestBase
    {
        void CompileAndRun(string xaml, CallbackExtensionCallback cb, IXamlXParentStackProviderV1 parentStack)
            => Compile(xaml).create(new DictionaryServiceProvider
            {
                [typeof(CallbackExtensionCallback)] = cb,
                [typeof(IXamlXParentStackProviderV1)] = parentStack
            });

        class ListParentsProvider : List<object>, IXamlXParentStackProviderV1
        {
            public IEnumerable<object> Parents => this;
        }
        
        [Fact]
        public void Parent_Stack_Should_Provide_Info_About_Parents()
        {
            var importedParents = new ListParentsProvider
            {
                "Parent1",
                "Parent2"
            };
            int num = 0;
            CompileAndRun(@"
<ServiceProviderTestsClass xmlns='test' Id='root' Property='{Callback}'>
    <ServiceProviderTestsClass.Child>
        <ServiceProviderTestsClass Id='direct' Property='{Callback}'/>
    </ServiceProviderTestsClass.Child>
    <ServiceProviderTestsClass Id='content' Property='{Callback}'/> 
</ServiceProviderTestsClass>", sp =>
            {
                //Manual unrolling of enumerable, useful for state tracking
                var stack = new List<object>();
                var parentsEnumerable = sp.GetService<IXamlXParentStackProviderV1>().Parents;
                using (var parentsEnumerator = parentsEnumerable.GetEnumerator())
                {
                    while (parentsEnumerator.MoveNext())
                        stack.Add(parentsEnumerator.Current);
                }
                
                        
                Assert.Equal("Parent1", stack[stack.Count - 2]);
                Assert.Equal("Parent2", stack.Last());
                if (num == 0)
                {
                    Assert.Equal(3, stack.Count);
                    Assert.Equal("root", ((ServiceProviderTestsClass) stack[0]).Id);
                }
                else if (num == 1)
                {
                    Assert.Equal(4, stack.Count);
                    Assert.Equal("direct", ((ServiceProviderTestsClass) stack[0]).Id);
                    Assert.Equal("root", ((ServiceProviderTestsClass) stack[1]).Id);
                }
                else if (num == 2)
                {
                    Assert.Equal(4, stack.Count);
                    Assert.Equal("content", ((ServiceProviderTestsClass) stack[0]).Id);
                    Assert.Equal("root", ((ServiceProviderTestsClass) stack[1]).Id);
                }
                else
                {
                    throw new InvalidOperationException();
                }

                num++;
                
                return "Value";
            }, importedParents);
        }
        
        [Fact]
        public void TypeDescriptor_Stubs_Are_Somewhat_Usable()
        {
            CompileAndRun(@"<ServiceProviderTestsClass xmlns='test' Property='{Callback}'/>", sp =>
            {
                var tdc = (ITypeDescriptorContext) sp;
                Assert.Equal(tdc, sp.GetService<ITypeDescriptorContext>());
                Assert.Null(tdc.Instance);
                Assert.Null(tdc.Container);
                Assert.Null(tdc.PropertyDescriptor);
                Assert.Throws<NotSupportedException>(() => tdc.OnComponentChanging());
                Assert.Throws<NotSupportedException>(() => tdc.OnComponentChanged());
                
                return "Value";
            }, null);
        }

        public static void SetAttachedProperty(ServiceProviderTestsClass target, string value)
        {
            
        }
        
        [Fact]
        public void ProvideValueTarget_Provides_Info_About_Properties()
        {
            var num = 0;
            CompileAndRun(@"
<ServiceProviderTestsClass xmlns='test' 
    Property='{Callback}'
    ServiceProviderTests.AttachedProperty='{Callback}'
/>", sp =>
            {
                var pt = sp.GetService<ITestProvideValueTarget>();
                Assert.IsType<ServiceProviderTestsClass>(pt.TargetObject);
                if(num == 0)
                    Assert.Equal("Property", pt.TargetProperty);
                else if (num == 1)
                {
                    Assert.Equal("1", ((ServiceProviderTestsClass) pt.TargetObject).Property);
                    var mh = (RuntimeMethodHandle) pt.TargetProperty;
                    var me = MethodBase.GetMethodFromHandle(mh);
                    Assert.Equal(typeof(ServiceProviderTests), me.DeclaringType);
                    Assert.Equal("SetAttachedProperty", me.Name);
                }
                else
                    throw new InvalidOperationException();
                num++;
                return num.ToString();
            }, null);
            Assert.Equal(2, num);
        }


        class InnerProvider : IServiceProvider, ITestRootObjectProvider
        {
            private ITestRootObjectProvider _originalRootObjectProvider;
            public object RootObject => "Definitely not the root object";
            public object OriginalRootObject => _originalRootObjectProvider.RootObject;
            public InnerProvider(IServiceProvider parent)
            {
                _originalRootObjectProvider = parent.GetService<ITestRootObjectProvider>();
            }
            
            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(ITestRootObjectProvider))
                    return this;
                return null;
            }
        }

        public static IServiceProvider InnerProviderFactory(IServiceProvider outer) => new InnerProvider(outer);
        
        [Fact]
        public void Inner_Provider_Interception_Works()
        {

            Configuration.TypeMappings.InnerServiceProviderFactoryMethod =
                Configuration.TypeSystem.GetType(typeof(ServiceProviderTests).FullName)
                    .FindMethod(m => m.Name == "InnerProviderFactory");
                    
            CompileAndRun(@"<ServiceProviderTestsClass xmlns='test' Property='{Callback}'/>", sp =>
            {
                var rootProv = (InnerProvider)sp.GetService<ITestRootObjectProvider>();
                Assert.IsType<ServiceProviderTestsClass>(rootProv.OriginalRootObject);
                Assert.IsType<string>(rootProv.RootObject);
                
                return "Value";
            }, null);
        }


        [Fact]
        public void Namespace_Info_Should_Be_Preserved()
        {
            CompileAndRun(@"
<ServiceProviderTestsClass 
    xmlns='test'
    xmlns:clr1='clr-namespace:System.Collections.Generic;assembly=netstandard'
    xmlns:clr2='clr-namespace:Dummy;assembly=XamlParserTests'
    Property='{Callback}'/>", sp =>
            {
                var npl = sp.GetType().DeclaringType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
                var np = npl
                    .First(t => typeof(IXamlXXmlNamespaceInfoProviderV1).IsAssignableFrom(t));
                var f = np.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                var wtf = f[0].GetValue(null);
                var nsList = sp.GetService<IXamlXXmlNamespaceInfoProviderV1>().XmlNamespaces;
                // Direct calls without struct diff because of EntryPointNotFoundException issue before
                Assert.True(nsList.TryGetValue("clr1", out var xlst));
                Assert.Equal("System.Collections.Generic", xlst[0].ClrNamespace);
                Helpers.StructDiff(nsList,
                    new Dictionary<string, IReadOnlyList<XamlXXmlNamespaceInfoV1>>
                    {
                        [""] = new List<XamlXXmlNamespaceInfoV1>
                        {
                            new XamlXXmlNamespaceInfoV1
                            {
                                ClrNamespace = "XamlParserTests",
                                ClrAssemblyName = typeof(ServiceProviderTests).Assembly.GetName().Name
                            }
                        },
                        ["clr1"] = new List<XamlXXmlNamespaceInfoV1>
                        {
                            new XamlXXmlNamespaceInfoV1
                            {
                                ClrNamespace = "System.Collections.Generic",
                                ClrAssemblyName = "netstandard"
                            }
                        },
                        ["clr2"] = new List<XamlXXmlNamespaceInfoV1>
                        {
                            new XamlXXmlNamespaceInfoV1
                            {
                                ClrNamespace = "Dummy",
                                ClrAssemblyName = "XamlParserTests"
                            }
                        }
                    });
                
                return "Value";
            }, null);
        }
        
        [Fact]
        public void Uri_Context_Is_Usable()
        {
            bool ok = false;
            CompileAndRun(@"<ServiceProviderTestsClass xmlns='test' Property='{Callback}'/>", sp =>
            {
                Assert.Equal("http://example.com/", sp.GetService<ITestUriContext>().BaseUri.ToString());
                ok = true;
                return "Value";
            }, null);
            Assert.True(ok);
        }
        
        [Fact]
        public void Unknown_Services_Should_Return_null()
        {
            var res = (ServiceProviderTestsClass)CompileAndRun(
                @"<ServiceProviderTestsClass xmlns='test' Property='{UnknownServiceUsage Return=123}'/>");
            Assert.Equal("123", res.Property);
            
        }
    }
}