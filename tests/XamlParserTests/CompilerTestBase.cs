using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using XamlIl.Parsers;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace XamlParserTests
{
    public class CompilerTestBase
    {
        private readonly IXamlIlTypeSystem _typeSystem;
        public XamlIlTransformerConfiguration Configuration { get; }

        public CompilerTestBase(IXamlIlTypeSystem typeSystem)
        {
            _typeSystem = typeSystem;
            Configuration = new XamlIlTransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("XamlParserTests"),
                new XamlIlLanguageTypeMappings
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
        static object s_asmLock = new object();
        protected object CompileAndRun(string xaml) => Compile(xaml)();
        protected Func<object> Compile(string xaml)
        {
            var parsed = XDocumentXamlIlParser.Parse(xaml);
            var compiler = new XamlIlAstTransformationManager(Configuration, true);
            compiler.Transform(parsed.Root, parsed.NamespaceAliases);
            
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
            compiler.Compile(parsed.Root, gen);
            
            
            var created = t.CreateType();
            #if !NETCOREAPP
            dm.CreateGlobalFunctions();
            // Useful for debugging the actual MSIL, don't remove
            lock (s_asmLock)
                da.Save("testasm.dll");
            #endif
            var cb = (Func<object>) Delegate.CreateDelegate(typeof(Func<object>),
                created.GetMethod("Build", BindingFlags.Static | BindingFlags.Public));
            return cb;
        }
    }
}