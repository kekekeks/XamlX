using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess;

namespace Benchmarks
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			/**  Uncomment to test using performance profiler */
			if (args?.FirstOrDefault() == "profile")
			{
				//var benchmark = new LoadSimpleBenchmark();
				var benchmark = new LoadComplexBenchmark();
				benchmark.XamlPrecompiled();
				for (int i = 0; i < 1000; i++)
				{
					benchmark.XamlPrecompiled();
				}
				return;
			}
			/**/

			// BenchmarkSwitcher doesn't automatically exclude abstract benchmark classes
			var types = typeof(MainClass)
				.Assembly
				.GetExportedTypes()
				.Where(r => typeof(IXamlBenchmark).IsAssignableFrom(r) && !r.IsAbstract);


			var config = ManualConfig
				.Create(DefaultConfig.Instance)
				.With(MemoryDiagnoser.Default)
				.With(StatisticColumn.OperationsPerSecond)
				.With(RankColumn.Arabic);
			if (args?.FirstOrDefault() == "--inproc")
			{
				args = args.Skip(1).ToArray();
				config = config
					.With(Job.Default.With(InProcessToolchain.Instance));
			}
			else
				config =config
					.With(Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp22).AsBaseline().WithId("Core 2.2"))
					.With(Job.Default.With(Runtime.Mono));
				

			var switcher = new BenchmarkSwitcher(types.ToArray());
			switcher.Run(args, config);
		}
	}
}
