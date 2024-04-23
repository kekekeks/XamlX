#nullable enable

using System;
using System.Reflection;
using System.Reflection.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace XamlParserTests;

public partial class CompilerTestBase
{
    private static partial IXamlTypeSystem CreateTypeSystem()
    {
        // Force XamlX.Runtime to be loaded, or else it won't be included in the type system
        Assembly.Load("XamlX.Runtime");
        return new SreTypeSystem();
    }

    protected partial class TestCompiler
    {
        private AssemblyBuilder _assembly = null!;
        private ModuleBuilder _module = null!;

#if !NETCOREAPP && !NETSTANDARD
        private static readonly object s_assemblyLock = new();
#endif

        private partial void Initialize()
        {
#if !NETCOREAPP && !NETSTANDARD
            _assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.RunAndSave,
                System.IO.Directory.GetCurrentDirectory());
#else
            _assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.Run);
#endif

            _module = _assembly.DefineDynamicModule("testasm.dll");
        }

        private partial void CompleteCompilation()
        {
#if !NETCOREAPP && !NETSTANDARD
            _module.CreateGlobalFunctions();
            // Useful for debugging the actual MSIL, don't remove
            lock (s_assemblyLock)
                _assembly.Save("testasm.dll");
#endif
        }

        private partial RuntimeTypeBuilder CreateTypeBuilderCore(string name, bool isPublic)
        {
            var attributes = TypeAttributes.Class | (isPublic ? TypeAttributes.Public : TypeAttributes.NotPublic);
            var type = _module.DefineType(name, attributes);
            var typeBuilder = ((SreTypeSystem)_configuration.TypeSystem).CreateTypeBuilder(type);
            return new RuntimeTypeBuilder(typeBuilder, () => type.CreateType()!);
        }
    }
}
