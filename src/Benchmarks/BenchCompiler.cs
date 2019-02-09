using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Benchmarks
{
    public class ContentAttribute : Attribute
    {
        
    }
    public class BenchCompiler
    {
        public static Func<IServiceProvider, object> Compile(string xaml)
        {
            
            // Enforce everything to load
            foreach (var xt in typeof(BenchCompiler).Assembly.GetTypes())
            {
                xt.GetCustomAttributes();
                xt.GetInterfaces();
                foreach (var p in xt.GetProperties())
                    p.GetCustomAttributes();
            }
            var typeSystem = new SreTypeSystem();
            var configuration = new XamlXTransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("Benchmarks"),
                new XamlXLanguageTypeMappings(typeSystem)
                {
                    XmlnsAttributes =
                    {
                        typeSystem.GetType("Portable.Xaml.Markup.XmlnsDefinitionAttribute"),
                    },
                    ContentAttributes =
                    {
                        typeSystem.GetType("Benchmarks.ContentAttribute")
                    },
                    RootObjectProvider = typeSystem.GetType("Portable.Xaml.IRootObjectProvider")
                });
            var parsed = XDocumentXamlXParser.Parse(xaml);
            
            var compiler = new XamlXCompiler(configuration, true);
            compiler.Transform(parsed, parsed.NamespaceAliases);
            
            
            var parsedTsType = ((IXamlXAstValueNode) parsed.Root).Type.GetClrType();
            

            var da = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);

            var dm = da.DefineDynamicModule("testasm.dll");
            var t = dm.DefineType(Guid.NewGuid().ToString("N"), TypeAttributes.Public);
            
            var contextClass = XamlXContext.GenerateContextClass(((SreTypeSystem) typeSystem).CreateTypeBuilder(
                    dm.DefineType(t.Name + "_Context", TypeAttributes.Public)),
                typeSystem, configuration.TypeMappings, parsedTsType);
            
            var parserTypeBuilder = ((SreTypeSystem) typeSystem).CreateTypeBuilder(t);
            compiler.Compile(parsed.Root, parserTypeBuilder, contextClass, "Populate", "Build");
            
            var created = t.CreateType();

            
            var isp = Expression.Parameter(typeof(IServiceProvider));
            return Expression.Lambda<Func<IServiceProvider, object>>(
                Expression.Convert(Expression.Call(
                    created.GetMethod("Build"), isp), typeof(object)), isp).Compile();

        }
    }
}