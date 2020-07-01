using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace BenchmarksCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var target = Path.GetFullPath(args[0]);
            var refsPath = target + ".refs";
            var refs = File.ReadAllLines(refsPath).Concat(new[] {target});
            var typeSystem = new CecilTypeSystem(refs, target);
            var asm = typeSystem.GetAssembly(typeSystem.FindAssembly("Benchmarks"));
            var config = Benchmarks.BenchmarksXamlXConfiguration.Configure(typeSystem);
            var loadBench = asm.MainModule.Types.First(t => t.Name == "LoadBenchmark");
            var baseMethod = loadBench.Methods.First(m => m.Name == "LoadXamlPrecompiled");

            var ct = new TypeDefinition("_XamlRuntime", "XamlContext", TypeAttributes.Class,
                asm.MainModule.TypeSystem.Object);

            asm.MainModule.Types.Add(ct);

            var ctb = typeSystem.CreateTypeBuilder(ct);
            var compiler = new XamlILCompiler(
                config,
                new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>(),
                true);
            var  contextTypeDef = compiler.CreateContextType(ctb);
            
            asm.MainModule.Types.Add(ct);
            foreach (var lb in asm.MainModule.Types)
            {
                if (lb == loadBench)
                    continue;
                var bt = lb;
                while (bt != null && bt != loadBench)
                    bt = bt.BaseType?.Resolve();
                
                if (bt != loadBench)
                    continue;

                var loadMethod = lb.Methods.FirstOrDefault(m => m.Name == "LoadXamlPrecompiled");
                if (loadMethod != null)
                    lb.Methods.Remove(loadMethod);

                var resource = asm.MainModule.Resources.OfType<EmbeddedResource>()
                    .First(r => r.Name == lb.FullName + ".xml");
                
                var xml = Encoding.UTF8.GetString(resource.GetResourceData());
                while (xml[0] > 128)
                    xml = xml.Substring(1);
                
                var parsed = XamlX.Parsers.XDocumentXamlParser.Parse(xml);
                compiler.Transform(parsed);
                compiler.Compile(parsed, typeSystem.CreateTypeBuilder(lb), contextTypeDef,
                    "PopulateXamlPrecompiled", "LoadXamlPrecompiled",
                    "XamlXmlInfo", resource.Name, null);
                
                loadMethod = lb.Methods.First(m => m.Name == "LoadXamlPrecompiled");
                loadMethod.ReturnType = asm.MainModule.TypeSystem.Object;
                loadMethod.Overrides.Add(baseMethod);
                loadMethod.Body.OptimizeMacros();
                loadMethod.Attributes = (baseMethod.Attributes | MethodAttributes.NewSlot)^ MethodAttributes.NewSlot;
                loadMethod.HasThis = true;
                for(var c=0;c<loadMethod.Body.Instructions.Count; c++)
                    if (loadMethod.Body.Instructions[c].OpCode == OpCodes.Ldarg_0)
                        loadMethod.Body.Instructions[c] = Instruction.Create(OpCodes.Ldarg_1);
            }

            asm.Write(target);
        }
    }
}