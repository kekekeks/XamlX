using System;
using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.IO;

namespace Benchmarks
{
	public interface IXamlBenchmark
	{
		
	}
	public abstract class LoadBenchmark : IXamlBenchmark
	{
		public abstract string TestName { get; }

		protected Stream GetStream() => typeof(IXamlBenchmark).Assembly.GetManifestResourceStream("Benchmarks." + TestName);

		Portable.Xaml.XamlSchemaContext pxc;
		[Benchmark(Baseline = true)]
		public void PortableXaml()
		{
			pxc = pxc ?? new Portable.Xaml.XamlSchemaContext();
			using (var stream = GetStream())
				Portable.Xaml.XamlServices.Load(new Portable.Xaml.XamlXmlReader(stream, pxc));
		}
/*
		System.Xaml.XamlSchemaContext sxc;
		[Benchmark]
		public void SystemXaml()
		{
			sxc = sxc ?? new System.Xaml.XamlSchemaContext();
			using (var stream = GetStream())
				System.Xaml.XamlServices.Load(new System.Xaml.XamlXmlReader(stream, sxc));
		}
*/
		[Benchmark]
		public void PortableXamlNoCache()
		{
			using (var stream = GetStream())
				Portable.Xaml.XamlServices.Load(stream);
		}
/*
		[Benchmark]
		public void SystemXamlNoCache()
		{
			using (var stream = GetStream())
				System.Xaml.XamlServices.Load(stream);
		}
*/
		private Func<IServiceProvider, object> _compiled;
		[Benchmark]
		public void Xaml()
		{
			_compiled = _compiled ?? BenchCompiler.Compile(new StreamReader(GetStream()).ReadToEnd());
			_compiled(null);
		}

		public virtual object LoadXamlPrecompiled(IServiceProvider sp)
		{
			// This method will be overridden by Cecil
			throw new NotImplementedException();
		}
		
		[Benchmark]
		public void XamlPrecompiled()
		{
			LoadXamlPrecompiled(null);
		}
		
	}
}
