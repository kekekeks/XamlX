using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using XamlX.TypeSystem;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace XamlParserTests
{
    public partial class CompilerTestBase
    {
        private static partial IXamlTypeSystem CreateTypeSystem()
        {
            var self = typeof(CompilerTestBase).Assembly.GetModules()[0].FullyQualifiedName;
            #if USE_NETSTANDARD_BUILD
            var selfDir = Path.GetDirectoryName(self)!;
            var selfName = Path.GetFileName(self);
            self = Path.GetFullPath(Path.Combine(selfDir, "../netstandard2.0/", selfName));
            #endif
            var refsPath = self + ".refs";
            var refs = File.ReadAllLines(refsPath).Concat(new[] {self});
            return new CecilTypeSystem(refs, null);
        }

        protected partial class TestCompiler
        {
            private static readonly object s_assemblyLock = new();

            private AssemblyDefinition? _cecilAssembly;
            private Assembly? _runtimeAssembly;

            public AssemblyDefinition CecilAssembly
            {
                get => _cecilAssembly ??= CreateCecilAssembly();
                set => _cecilAssembly = value;
            }

            private AssemblyDefinition CreateCecilAssembly()
                => ((CecilTypeSystem)_configuration.TypeSystem).CreateAndRegisterAssembly(
                    "TestAsm",
                    new Version(1, 0),
                    ModuleKind.Dll);

            private partial void Initialize()
            {
            }

            private partial RuntimeTypeBuilder CreateTypeBuilderCore(string name, bool isPublic)
            {
                const string ns = "TestXaml";

                var attributes = TypeAttributes.Class | (isPublic ? TypeAttributes.Public : TypeAttributes.NotPublic);
                var type = new TypeDefinition(ns, name, attributes, CecilAssembly.MainModule.TypeSystem.Object);
                CecilAssembly.MainModule.Types.Add(type);
                var typeBuilder = ((CecilTypeSystem)_configuration.TypeSystem).CreateTypeBuilder(type);

                return new RuntimeTypeBuilder(typeBuilder, () => GetRuntimeType($"{ns}.{name}"));
            }

            public Type GetRuntimeType(string name)
                => _runtimeAssembly is null
                    ? throw new InvalidOperationException($"{nameof(Compile)}() hasn't been called")
                    : _runtimeAssembly.GetType(name, throwOnError: true)!;

            private partial void CompleteCompilation()
            {
                using var memoryStream = new MemoryStream();

                CecilAssembly.Write(memoryStream);
                var data = memoryStream.ToArray();

                lock (s_assemblyLock)
                    File.WriteAllBytes("testasm.dll", data);

                _runtimeAssembly = Assembly.Load(data);
            }
        }
    }
}