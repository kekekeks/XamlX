using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
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
                new XamlXLanguageTypeMappings
                {
                    XmlnsAttributes =
                    {
                        typeSystem.FindType("XamlParserTests.XmlnsDefinitionAttribute"),

                    },
                    ContentAttributes =
                    {
                        typeSystem.FindType("XamlParserTests.ContentAttribute")
                    }
                }
            );
        }

        public CompilerTestBase() : this(new SreTypeSystem())
        {
            
        }

        protected object CompileAndRun(string xaml)
        {
            var root = XDocumentXamlXParser.Parse(xaml);
            var compiler = new XamlXAstTransformationManager(Configuration, true);
            compiler.Transform(root);
            
            #if !NETCOREAPP
            
            var da = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.RunAndSave,
                Directory.GetCurrentDirectory());
            #else
            var da = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            #endif

            var dm = da.DefineDynamicModule("testasm.dll");
            var t = dm.DefineType(Guid.NewGuid().ToString("N"), TypeAttributes.Public);
            var m = t.DefineMethod("Build", MethodAttributes.Static | MethodAttributes.Public,
                typeof(object), new Type[0]);
            var gen = ((SreTypeSystem) _typeSystem).CreateCodeGen(m);
            compiler.Compile(root, gen);
            
            
            var created = t.CreateType();
            #if !NETCOREAPP
            dm.CreateGlobalFunctions();
            da.Save( "testasm.dll");
            #endif
            var cb = (Func<object>) Delegate.CreateDelegate(typeof(Func<object>),
                created.GetMethod("Build", BindingFlags.Static | BindingFlags.Public));
            return cb();
        }
    }
}