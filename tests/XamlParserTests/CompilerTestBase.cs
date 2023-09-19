using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;
using XamlX.IL;
using XamlX.Emit;

namespace XamlParserTests
{
    public partial class CompilerTestBase
    {
        private readonly IXamlTypeSystem _typeSystem;
        public TransformerConfiguration Configuration { get; }

        private CompilerTestBase(IXamlTypeSystem typeSystem)
        {
            _typeSystem = typeSystem;
            Configuration = new TransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("XamlParserTests"),
                new XamlLanguageTypeMappings(typeSystem)
                {
                    XmlnsAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.XmlnsDefinitionAttribute"),

                    },
                    ContentAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.ContentAttribute")
                    },
                    WhitespaceSignificantCollectionAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.WhitespaceSignificantCollectionAttribute")
                    },
                    TrimSurroundingWhitespaceAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.TrimSurroundingWhitespaceAttribute")
                    },
                    UsableDuringInitializationAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.UsableDuringInitializationAttribute")
                    },
                    DeferredContentPropertyAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.DeferredContentAttribute")
                    },
                    RootObjectProvider = typeSystem.GetType("XamlParserTests.ITestRootObjectProvider"),
                    UriContextProvider = typeSystem.GetType("XamlParserTests.ITestUriContext"),
                    ProvideValueTarget = typeSystem.GetType("XamlParserTests.ITestProvideValueTarget"),
                    ParentStackProvider = typeSystem.GetType("XamlX.Runtime.IXamlParentStackProviderV1"),
                    XmlNamespaceInfoProvider = typeSystem.GetType("XamlX.Runtime.IXamlXmlNamespaceInfoProviderV1"),
                    IAddChild = typeSystem.GetType("XamlParserTests.IAddChild"),
                    IAddChildOfT = typeSystem.GetType("XamlParserTests.IAddChild`1")
                }
            ); ;
        }

        protected object CompileAndRun(string xaml, IServiceProvider prov = null) => Compile(xaml).create(prov);

        protected object CompileAndPopulate(string xaml, IServiceProvider prov = null, object instance = null)
            => Compile(xaml).create(prov);
        XamlDocument Compile(IXamlTypeBuilder<IXamlILEmitter> builder, IXamlType context, string xaml, bool generateBuildMethod)
        {
            var parsed = XamlParser.Parse(xaml);
            var compiler = new XamlILCompiler(
                Configuration,
                new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>(),
                true)
            {
                EnableIlVerification = true
            };
            compiler.Transform(parsed);
            compiler.Compile(parsed, builder, context, "Populate", generateBuildMethod ? "Build" : null,
                "XamlNamespaceInfo",
                "http://example.com/", null);
            return parsed;
        }
        static object s_asmLock = new object();
        
#if !CECIL
        private static SreTypeSystem CreateTypeSystem()
        {
            // Force XamlX.Runtime to be loaded, or else it won't be included in the type system
            Assembly.Load("XamlX.Runtime");
            return new SreTypeSystem();
        }

        public CompilerTestBase() : this(CreateTypeSystem())
        {
            
        }
        
        protected (Func<IServiceProvider, object> create, Action<IServiceProvider, object> populate) Compile(string xaml, bool generateBuildMethod = true)
        {
            #if !NETCOREAPP && !NETSTANDARD
            var da = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.RunAndSave,
                Directory.GetCurrentDirectory());
            #else
            var da = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            #endif

            var dm = da.DefineDynamicModule("testasm.dll");
            var t = dm.DefineType(Guid.NewGuid().ToString("N"), TypeAttributes.Public);
            var ct = dm.DefineType(t.Name + "Context");
            var ctb = ((SreTypeSystem)_typeSystem).CreateTypeBuilder(ct);
            var contextTypeDef =
                XamlILContextDefinition.GenerateContextClass(
                    ctb,
                    _typeSystem,
                    Configuration.TypeMappings,
                    new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>());
            
            
            var parserTypeBuilder = ((SreTypeSystem) _typeSystem).CreateTypeBuilder(t);

            var parsed = Compile(parserTypeBuilder, contextTypeDef, xaml, generateBuildMethod);

            var created = t.CreateTypeInfo();
            #if !NETCOREAPP && !NETSTANDARD
            dm.CreateGlobalFunctions();
            // Useful for debugging the actual MSIL, don't remove
            lock (s_asmLock)
                da.Save("testasm.dll");
            #endif

            return GetCallbacks(created);
        }
        #endif

        (Func<IServiceProvider, object> create, Action<IServiceProvider, object> populate)
            GetCallbacks(Type created)
        {
            var isp = Expression.Parameter(typeof(IServiceProvider));
            var createCb = created.GetMethod("Build") is {} buildMethod
                ? Expression.Lambda<Func<IServiceProvider, object>>(
                    Expression.Convert(Expression.Call(buildMethod, isp), typeof(object)), isp).Compile()
                : null;
            
            var epar = Expression.Parameter(typeof(object));
            var populate = created.GetMethod("Populate");
            isp = Expression.Parameter(typeof(IServiceProvider));
            var populateCb = Expression.Lambda<Action<IServiceProvider, object>>(
                Expression.Call(populate, isp, Expression.Convert(epar, populate.GetParameters()[1].ParameterType)),
                isp, epar).Compile();
            
            return (createCb, populateCb);
        }
        
    }
}