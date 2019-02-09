using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

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
				benchmark.XamlIl();
				for (int i = 0; i < 1000; i++)
				{
					benchmark.XamlIl();
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
				.With(Job.Default)
				.With(MemoryDiagnoser.Default)
				.With(StatisticColumn.OperationsPerSecond)
				.With(RankColumn.Arabic);

			var switcher = new BenchmarkSwitcher(types.ToArray());
			switcher.Run(args, config);
		}
	}
}
