using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using XamlX.Emit;
using XamlX.TypeSystem;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace XamlParserTests
{
    public partial class CompilerTestBase
    {
#if NETFRAMEWORK
        private static readonly string s_selfDirectory;
        private static readonly ConcurrentDictionary<string, Assembly> s_nameToAssembly;

        static CompilerTestBase()
        {
            // TODO: It's the hack for VS tests
            s_selfDirectory = Path.GetDirectoryName(typeof(CompilerTestBase).Assembly.Location);
            s_nameToAssembly = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name.Split(',')[0];
            if (!s_nameToAssembly.TryGetValue(name, out var assembly))
            {
                assembly = Assembly.LoadFile(Path.Combine(s_selfDirectory, name + ".dll"));
                s_nameToAssembly.TryAdd(name, assembly);
            }
            return assembly;
        }
#endif

        static CecilTypeSystem CreateCecilTypeSystem()
        {
            var self = typeof(CompilerTestBase).Assembly.GetModules()[0].FullyQualifiedName;
#if USE_NETSTANDARD_BUILD
            var selfDir = Path.GetDirectoryName(self);
            var selfName = Path.GetFileName(self);
            self = Path.GetFullPath(Path.Combine(selfDir, "../netstandard2.0/", selfName));
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