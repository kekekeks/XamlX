using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;
using XamlX.IL;
using XamlX.Emit;

namespace XamlParserTests
{
    public partial class CompilerTestBase
    {
        public CompilerTestBase() : this(CreateTypeSystem())
        {
        }

        public TransformerConfiguration Configuration { get; }
        public List<XamlDiagnostic> Diagnostics { get; } = new();

        private CompilerTestBase(IXamlTypeSystem typeSystem)
        {
            Configuration = new TransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("XamlParserTests"),
                new XamlLanguageTypeMappings(typeSystem)
                {
                    XmlnsAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.XmlnsDefinitionAttribute"),

                    },
                    ContentAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.ContentAttribute")
                    },
                    WhitespaceSignificantCollectionAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.WhitespaceSignificantCollectionAttribute")
                    },
                    TrimSurroundingWhitespaceAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.TrimSurroundingWhitespaceAttribute")
                    },
                    UsableDuringInitializationAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.UsableDuringInitializationAttribute")
                    },
                    DeferredContentPropertyAttributes =
                    {
                        typeSystem.GetType("XamlParserTests.DeferredContentAttribute")
                    },
                    RootObjectProvider = typeSystem.GetType("XamlParserTests.ITestRootObjectProvider"),
                    UriContextProvider = typeSystem.GetType("XamlParserTests.ITestUriContext"),
                    ProvideValueTarget = typeSystem.GetType("XamlParserTests.ITestProvideValueTarget"),
                    ParentStackProvider = typeSystem.GetType("XamlX.Runtime.IXamlParentStackProviderV1"),
                    XmlNamespaceInfoProvider = typeSystem.GetType("XamlX.Runtime.IXamlXmlNamespaceInfoProviderV1"),
                    IAddChild = typeSystem.GetType("XamlParserTests.IAddChild"),
                    IAddChildOfT = typeSystem.GetType("XamlParserTests.IAddChild`1")
                },
                diagnosticsHandler: new XamlDiagnosticsHandler
                {
                    HandleDiagnostic = diagnostic =>
                    {
                        Diagnostics.Add(diagnostic);
                        return diagnostic.Severity;
                    }
                }
            );
        }

        protected TestCompiler CreateTestCompiler()
            => new(Configuration, Diagnostics);

        protected (Func<IServiceProvider?, object>? create, Action<IServiceProvider?, object?> populate)
            Compile(string xaml, bool generateBuildMethod = true)
        {
            var compiler = CreateTestCompiler();
            return compiler.Compile(xaml, generateBuildMethod);
        }

        protected object CompileAndRun(string xaml, IServiceProvider? prov = null)
            => Compile(xaml).create!(prov);

        protected void CompileAndPopulate(string xaml, IServiceProvider? prov = null, object? instance = null)
            => Compile(xaml, false).populate(prov, instance);

        public XamlDocument Transform(string xaml)
        {
            var parsed = XamlParser.Parse(xaml);
            var compiler = new XamlILCompiler(
                Configuration,
                new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>(),
                true)
            {
                EnableIlVerification = true
            };
            compiler.Transform(parsed);
            return parsed;
        }

        private static partial IXamlTypeSystem CreateTypeSystem();

        protected sealed partial class TestCompiler
        {
            private readonly TransformerConfiguration _configuration;
            private readonly List<XamlDiagnostic> _diagnostics;
            private readonly List<RuntimeTypeBuilder> _typeBuilders = new();

            public XamlILCompiler IlCompiler { get; }

            public TestCompiler(TransformerConfiguration configuration, List<XamlDiagnostic> diagnostics)
            {
                _configuration = configuration;
                _diagnostics = diagnostics;

                Initialize();

                IlCompiler = new XamlILCompiler(
                    configuration,
                    new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>(),
                    true)
                {
                    EnableIlVerification = true
                };
            }

            public RuntimeTypeBuilder CreateTypeBuilder(string name, bool isPublic)
            {
                var typeBuilder = CreateTypeBuilderCore(name, isPublic);
                _typeBuilders.Add(typeBuilder);
                return typeBuilder;
            }

            public (Func<IServiceProvider?, object>? create, Action<IServiceProvider?, object?> populate)
                Compile(string xaml, bool generateBuildMethod = true, RuntimeTypeBuilder? parsedTypeBuilder = null)
            {
                parsedTypeBuilder ??= CreateTypeBuilder(Guid.NewGuid().ToString("N"), true);
                var contextTypeBuilder = CreateTypeBuilder(parsedTypeBuilder.XamlTypeBuilder.Name + "Context", false);

                var contextTypeDef = XamlILContextDefinition.GenerateContextClass(
                    contextTypeBuilder.XamlTypeBuilder,
                    _configuration.TypeSystem,
                    _configuration.TypeMappings,
                    new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>());

                var document = XamlParser.Parse(xaml);
                IlCompiler.Transform(document);
                _diagnostics.ThrowExceptionIfAnyError();

                IlCompiler.Compile(
                    document,
                    parsedTypeBuilder.XamlTypeBuilder,
                    contextTypeDef,
                    "Populate",
                    generateBuildMethod ? "Build" : null,
                    "XamlNamespaceInfo",
                    "http://example.com/", null);

                foreach (var typeBuilder in _typeBuilders)
                    typeBuilder.XamlTypeBuilder.CreateType();

                CompleteCompilation();

                return GetCallbacks(parsedTypeBuilder.RuntimeType);
            }

            private static (Func<IServiceProvider?, object>? create, Action<IServiceProvider?, object?> populate)
                GetCallbacks(Type created)
            {
                var isp = Expression.Parameter(typeof(IServiceProvider));
                var createCb = created.GetMethod("Build") is {} buildMethod
                    ? Expression.Lambda<Func<IServiceProvider?, object>>(
                        Expression.Convert(Expression.Call(buildMethod, isp), typeof(object)), isp).Compile()
                    : null;

                var epar = Expression.Parameter(typeof(object));
                var populate = created.GetMethod("Populate")!;
                isp = Expression.Parameter(typeof(IServiceProvider));
                var populateCb = Expression.Lambda<Action<IServiceProvider?, object?>>(
                    Expression.Call(populate, isp, Expression.Convert(epar, populate.GetParameters()[1].ParameterType)),
                    isp, epar).Compile();

                return (createCb, populateCb);
            }

            private partial void Initialize();

            private partial void CompleteCompilation();

            private partial RuntimeTypeBuilder CreateTypeBuilderCore(string name, bool isPublic);
        }

        protected sealed class RuntimeTypeBuilder
        {
            private readonly Func<Type> _createRuntimeType;
            private Type? _runtimeType;

            public RuntimeTypeBuilder(IXamlTypeBuilder<IXamlILEmitter> xamlTypeBuilder, Func<Type> createRuntimeType)
            {
                XamlTypeBuilder = xamlTypeBuilder;
                _createRuntimeType = createRuntimeType;
            }

            public IXamlTypeBuilder<IXamlILEmitter> XamlTypeBuilder { get; }

            public Type RuntimeType
                => _runtimeType ??= _createRuntimeType();
        }
    }
}