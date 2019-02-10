using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlParserTests
{
    public class CompilerTestBase
    {
        private readonly IXamlXTypeSystem _typeSystem;
        public XamlXTransformerConfiguration Configuration { get; }

        public CompilerTestBase(IXamlXTypeSystem typeSystem)
        {
            _typeSystem = typeSystem;
            Configuration = new XamlXTransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("XamlParserTests"),
                new XamlXLanguageTypeMappings(typeSystem)
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
                    ParentStackProvider = typeSystem.GetType("XamlX.Runtime.IXamlXParentStackProviderV1"),
                    XmlNamespaceInfoProvider = typeSystem.GetType("XamlX.Runtime.IXamlXXmlNamespaceInfoProviderV1"),
                    MarkupExtensionCustomResultHandler = typeSystem.GetType("XamlParserTests.CompilerTestBase")
                        .Methods.First(m => m.Name == "ApplyNonMatchingMarkupExtension")
                }
            );
        }

        public static void ApplyNonMatchingMarkupExtension(object target, object property, IServiceProvider prov,
            object value)
        {
            throw new InvalidCastException();
        }

        public CompilerTestBase() : this(new SreTypeSystem())
        {
            
        }
        static object s_asmLock = new object();
        protected object CompileAndRun(string xaml, IServiceProvider prov = null) => Compile(xaml).create(prov);

        protected object CompileAndPopulate(string xaml, IServiceProvider prov = null, object instance = null)
            => Compile(xaml).create(prov);
        
        protected (Func<IServiceProvider, object> create, Action<IServiceProvider, object> populate) Compile(string xaml)
        {
            var parsed = XDocumentXamlXParser.Parse(xaml);
            
            var compiler = new XamlXCompiler(Configuration, true);
            compiler.Transform(parsed, parsed.NamespaceAliases);
            
            
            var parsedTsType = ((IXamlXAstValueNode) parsed.Root).Type.GetClrType();
            var parsedType =
                ((SreTypeSystem) _typeSystem).GetType(parsedTsType);
            
            #if !NETCOREAPP
            
            var da = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.RunAndSave,
                Directory.GetCurrentDirectory());
            #else
            var da = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            #endif

            var dm = da.DefineDynamicModule("testasm.dll");
            var t = dm.DefineType(Guid.NewGuid().ToString("N"), TypeAttributes.Public);
            
            var parserTypeBuilder = ((SreTypeSystem) _typeSystem).CreateTypeBuilder(t);
            compiler.Compile(parsed, parserTypeBuilder, "Populate", "Build",
                "XamlXRuntimeContext", "XamlXNamespaceInfo",
                "http://example.com/");
            
            var created = t.CreateType();
            #if !NETCOREAPP
            dm.CreateGlobalFunctions();
            // Useful for debugging the actual MSIL, don't remove
            lock (s_asmLock)
                da.Save("testasm.dll");
            #endif
            
            
            
            var isp = Expression.Parameter(typeof(IServiceProvider));
            var createCb = Expression.Lambda<Func<IServiceProvider, object>>(
                Expression.Convert(Expression.Call(
                    created.GetMethod("Build"), isp), typeof(object)), isp).Compile();
            

            var epar = Expression.Parameter(typeof(object));
            isp = Expression.Parameter(typeof(IServiceProvider));
            var populateCb = Expression.Lambda<Action<IServiceProvider, object>>(
                Expression.Call(created.GetMethod("Populate"), isp, Expression.Convert(epar, parsedType)),
                isp, epar).Compile();
            
            return (createCb, populateCb);
        }
        
        
    }
}