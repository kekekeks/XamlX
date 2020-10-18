using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using XamlX.Emit;
using XamlX.Runtime;
using XamlX.TypeSystem;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace XamlParserTests
{
    public partial class CompilerTestBase
    {
        private static readonly string s_selfDirectory;
        private static readonly IReadOnlyDictionary<string, Assembly> s_nameToAssembly;

        static CompilerTestBase()
        {
            // TODO: It's the hack for VS tests
            var selfAssembly = typeof(CompilerTestBase).Assembly;

            s_selfDirectory = Path.GetDirectoryName(selfAssembly.Location);
            var baseDirectory = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            if (s_selfDirectory.Equals(baseDirectory, StringComparison.OrdinalIgnoreCase))
                return;

            var xamlRuntimeAssembly = typeof(IXamlParentStackProviderV1).Assembly;
            var testClassesAssembly = typeof(SimpleClass).Assembly;
            s_nameToAssembly = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase)
            {
                { selfAssembly.FullName.Split(',')[0], selfAssembly },
                { xamlRuntimeAssembly.FullName.Split(',')[0], xamlRuntimeAssembly },
                { testClassesAssembly.FullName.Split(',')[0], testClassesAssembly },
            };

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name.Split(',')[0];
            return s_nameToAssembly[name];
        }

        static CecilTypeSystem CreateCecilTypeSystem()
        {
            var self = typeof(SimpleClass).Assembly.GetModules()[0].FullyQualifiedName;
            var selfName = Path.GetFileName(self);
            var configuration = Path.GetFileName(Path.GetDirectoryName(s_selfDirectory));
            var dir = Path.Combine(s_selfDirectory, $"../../../../TestClasses/bin/{configuration}");
#if USE_NETSTANDARD_BUILD
            self = Path.GetFullPath(Path.Combine(dir, "netstandard2.0", selfName));
#else
            self = Path.GetFullPath(Path.Combine(dir, Path.GetFileName(s_selfDirectory), selfName));
#endif
            var refsPath = self + ".refs";
            var refs = File.ReadAllLines(refsPath).Concat(new[] {self});
            return new CecilTypeSystem(refs, null);
        }

        public CompilerTestBase() : this(CreateCecilTypeSystem())
        {
        }
        
        
        protected (Func<IServiceProvider, object> create, Action<IServiceProvider, object> populate) Compile(
            string xaml)
        {
            var ts = (CecilTypeSystem) (_typeSystem);
            var asm = ts.CreateAndRegisterAssembly("TestAsm", new Version(1, 0),
                ModuleKind.Dll);

            var def = new TypeDefinition("TestXaml", "Xaml",
                TypeAttributes.Class | TypeAttributes.Public, asm.MainModule.TypeSystem.Object);

            var ct = new TypeDefinition("TestXaml", "XamlContext", TypeAttributes.Class,
                asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(ct);
            var ctb = ((CecilTypeSystem)_typeSystem).CreateTypeBuilder(ct);
            var contextTypeDef = XamlX.IL.XamlILContextDefinition.GenerateContextClass(ctb, _typeSystem, Configuration.TypeMappings, new XamlLanguageEmitMappings<XamlX.IL.IXamlILEmitter, XamlX.IL.XamlILNodeEmitResult>());
            
            asm.MainModule.Types.Add(def);


            var tb = ts.CreateTypeBuilder(def);
            Compile(tb, contextTypeDef, xaml);
            
            var ms = new MemoryStream();
            asm.Write(ms);
            var data = ms.ToArray();
#if CHECK_MSIL
            lock (s_asmLock)
                File.WriteAllBytes("testasm.dll", data);
#endif
            
            var loaded = Assembly.Load(data);
            var t = loaded.GetType("TestXaml.Xaml");

            return GetCallbacks(t);
        }
    }
}