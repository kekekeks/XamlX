using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.Parsers;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace XamlParserTests
{
    public partial class CompilerTestBase
    {
        private readonly IXamlIlTypeSystem _typeSystem;
        public XamlIlTransformerConfiguration Configuration { get; }

        private CompilerTestBase(IXamlIlTypeSystem typeSystem)
        {
            _typeSystem = typeSystem;
            Configuration = new XamlIlTransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("XamlParserTests"),
                new XamlIlLanguageTypeMappings(typeSystem)
                {
                    XmlnsAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.XmlnsDefinitionAttribute"),

                    },
                    ContentAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.ContentAttribute")
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
                    ParentStackProvider = typeSystem.GetType("XamlIl.Runtime.IXamlIlParentStackProviderV1"),
                    XmlNamespaceInfoProvider = typeSystem.GetType("XamlIl.Runtime.IXamlIlXmlNamespaceInfoProviderV1")
                }
            );
        }

        protected object CompileAndRun(string xaml, IServiceProvider prov = null) => Compile(xaml).create(prov);

        protected object CompileAndPopulate(string xaml, IServiceProvider prov = null, object instance = null)
            => Compile(xaml).create(prov);
        XamlIlDocument Compile(IXamlIlTypeBuilder builder, IXamlIlType context, string xaml)
        {
            var parsed = XDocumentXamlIlParser.Parse(xaml);
            var compiler = new XamlIlCompiler(Configuration, true);
            compiler.Transform(parsed);
            compiler.Compile(parsed, builder, context, "Populate", "Build",
                "XamlIlNamespaceInfo",
                "http://example.com/", null);
            return parsed;
        }
        static object s_asmLock = new object();
        
#if !CECIL
        public CompilerTestBase() : this(new SreTypeSystem())
        {
            
        }
        
        protected (Func<IServiceProvider, object> create, Action<IServiceProvider, object> populate) Compile(string xaml)
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
                XamlIlContextDefinition.GenerateContextClass(ctb, _typeSystem, Configuration.TypeMappings);
            
            
            var parserTypeBuilder = ((SreTypeSystem) _typeSystem).CreateTypeBuilder(t);

            var parsed = Compile(parserTypeBuilder, contextTypeDef, xaml);

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
            var createCb = Expression.Lambda<Func<IServiceProvider, object>>(
                Expression.Convert(Expression.Call(
                    created.GetMethod("Build"), isp), typeof(object)), isp).Compile();
            
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