using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.IL.Emitters;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;
using Xunit;

namespace XamlParserTests
{
    public interface ISpecialHandling<T1, T2>
    {
        T1 Value { get; }

        SpecialHandler<T2> SpecialHandler { get; }
    }

    public class DynamicSettersClass<T1, T2> : ISpecialHandling<T1, T2>
    {
        private T1 _value;

        internal bool IsValueSet;

        public T1 Value
        {
            get => _value;
            set
            {
                _value = value;
                IsValueSet = true;
            }
        }

        public SpecialHandler<T2> SpecialHandler { get; } = new();
    }

    public class PrivateDynamicSettersClass : ISpecialHandling<int, string>
    {
        private int _value;

        internal bool IsValueSet;

        private int Value
        {
            get => _value;
            set
            {
                _value = value;
                IsValueSet = true;
            }
        }

        int ISpecialHandling<int, string>.Value
            => Value;

        public SpecialHandler<string> SpecialHandler { get; } = new();
    }

    public class ProtectedDynamicSettersClass : ISpecialHandling<int, string>
    {
        private int _value;

        internal bool IsValueSet;

        protected int Value
        {
            get => _value;
            set
            {
                _value = value;
                IsValueSet = true;
            }
        }

        int ISpecialHandling<int, string>.Value
            => Value;

        public SpecialHandler<string> SpecialHandler { get; } = new();
    }

    public class SpecialHandler<T>
    {
        internal bool Called;
        internal T Value;

        public void Handle(T value)
        {
            Called = true;
            Value = value;
        }
    }

    public class DynamicProvider
    {
        public enum ProvidedValueType { Null, String, Uri, Int32, TimeSpan, DateTime }

        public ProvidedValueType ProvidedValue { get; set; }

        public object ProvideValue()
        {
            return ProvidedValue switch
            {
                ProvidedValueType.Null => null,
                ProvidedValueType.String => "foo",
                ProvidedValueType.Uri => new Uri("https://avaloniaui.net/"),
                ProvidedValueType.Int32 => 1234,
                ProvidedValueType.TimeSpan => new TimeSpan(12, 34, 56, 789),
                ProvidedValueType.DateTime => DateTime.Now,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public class DynamicSettersTests : CompilerTestBase
    {
        private readonly TestCompiler _compiler;

        public DynamicSettersTests()
        {
            _compiler = CreateTestCompiler();

            _compiler.IlCompiler.Transformers.Insert(
                _compiler.IlCompiler.Transformers.FindIndex(x => x is PropertyReferenceResolver) + 1,
                new SpecialTransformer());
        }

        private object CompileAndRunWithSpecialTransformer(string xaml)
            => _compiler.Compile(xaml).create(null);

        [Fact]
        public void Dynamic_Setter_With_Reference_Types_And_Null_Argument_Should_Match_First_Setter()
        {
            var result = (DynamicSettersClass<string, Uri>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:String,sys:Uri' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Null}' />
");
            Assert.True(result.IsValueSet);
            Assert.Null(result.Value);
            Assert.False(result.SpecialHandler.Called);
            Assert.Null(result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Reference_Types_And_Typed_Argument_Should_Match_1()
        {
            var result = (DynamicSettersClass<string, Uri>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:String,sys:Uri' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=String}' />
");
            Assert.True(result.IsValueSet);
            Assert.Equal("foo", result.Value);
            Assert.False(result.SpecialHandler.Called);
            Assert.Null(result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Reference_Types_And_Typed_Argument_Should_Match_2()
        {
            var result = (DynamicSettersClass<string, Uri>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:String,sys:Uri' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Uri}' />
");
            Assert.False(result.IsValueSet);
            Assert.Null(result.Value);
            Assert.True(result.SpecialHandler.Called);
            Assert.Equal(new Uri("https://avaloniaui.net/"), result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Types_And_Null_Argument_Should_Throw_NullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:TimeSpan' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Null}' />
"));
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Types_And_Typed_Argument_Should_Match_1()
        {
            var result = (DynamicSettersClass<int, TimeSpan>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:TimeSpan' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Int32}' />
");
            Assert.True(result.IsValueSet);
            Assert.Equal(1234, result.Value);
            Assert.False(result.SpecialHandler.Called);
            Assert.Equal(default, result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Types_And_Typed_Argument_Should_Match_2()
        {
            var result = (DynamicSettersClass<int, TimeSpan>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:TimeSpan' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=TimeSpan}' />
");
            Assert.False(result.IsValueSet);
            Assert.Equal(default, result.Value);
            Assert.True(result.SpecialHandler.Called);
            Assert.Equal(new TimeSpan(12, 34, 56, 789), result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Types_And_Null_Argument_Should_Match_First_Setter()
        {
            var result = (DynamicSettersClass<int?, TimeSpan?>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:Nullable(sys:TimeSpan)' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Null}' />
");
            Assert.True(result.IsValueSet);
            Assert.Null(result.Value);
            Assert.False(result.SpecialHandler.Called);
            Assert.Null(result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Types_And_Typed_Argument_Should_Match_1()
        {
            var result = (DynamicSettersClass<int?, TimeSpan?>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:Nullable(sys:TimeSpan)' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Int32}' />
");
            Assert.True(result.IsValueSet);
            Assert.Equal(1234, result.Value);
            Assert.False(result.SpecialHandler.Called);
            Assert.Equal(default, result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Types_And_Typed_Argument_Should_Match_2()
        {
            var result = (DynamicSettersClass<int?, TimeSpan?>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:Nullable(sys:TimeSpan)' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=TimeSpan}' />
");
            Assert.False(result.IsValueSet);
            Assert.Equal(default, result.Value);
            Assert.True(result.SpecialHandler.Called);
            Assert.Equal(new TimeSpan(12, 34, 56, 789), result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Type_And_Reference_Type_And_Null_Argument_Should_Match_Reference()
        {
            var result = (DynamicSettersClass<int, string>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Null}' />
");
            Assert.False(result.IsValueSet);
            Assert.Equal(default, result.Value);
            Assert.True(result.SpecialHandler.Called);
            Assert.Null(result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Value_Type_And_Reference_Type_And_Value_Type_Argument_Should_Match_Value_Type()
        {
            var result = (DynamicSettersClass<int, string>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Int32}' />
");
            Assert.True(result.IsValueSet);
            Assert.Equal(1234, result.Value);
            Assert.False(result.SpecialHandler.Called);
            Assert.Null(result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Type_And_Reference_Type_And_Null_Argument_Should_Match_First_Setter()
        {
            var result = (DynamicSettersClass<int?, string>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Null}' />
");
            Assert.True(result.IsValueSet);
            Assert.Null(result.Value);
            Assert.False(result.SpecialHandler.Called);
            Assert.Null(result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Type_And_Reference_Type_And_Value_Type_Argument_Should_Match_Nullable_Value_Type()
        {
            var result = (DynamicSettersClass<int?, string>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=Int32}' />
");
            Assert.True(result.IsValueSet);
            Assert.Equal(1234, result.Value);
            Assert.False(result.SpecialHandler.Called);
            Assert.Null(result.SpecialHandler.Value);
        }

        [Fact]
        public void Dynamic_Setter_With_Nullable_Value_Type_And_Reference_Type_And_Reference_Type_Argument_Should_Match_Reference_Type()
        {
            var result = (DynamicSettersClass<int?, string>) CompileAndRunWithSpecialTransformer(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Nullable(sys:Int32),sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=String}' />
");
            Assert.False(result.IsValueSet);
            Assert.Equal(default, result.Value);
            Assert.True(result.SpecialHandler.Called);
            Assert.Equal("foo", result.SpecialHandler.Value);
        }

        [Theory]
        [InlineData("sys:Int32", "sys:TimeSpan")]
        [InlineData("sys:String", "sys:Uri")]
        [InlineData("sys:Nullable(sys:Int32)", "sys:Nullable(sys:TimeSpan)")]
        [InlineData("sys:Int32", "sys:String")]
        [InlineData("sys:Nullable(sys:Int32)", "sys:String")]
        public void Dynamic_Setter_With_Mismatched_Argument_Should_Throw_InvalidCastException(string type1, string type2)
        {
            Assert.Throws<InvalidCastException>(() => CompileAndRunWithSpecialTransformer($@"
<DynamicSettersClass 
    x:TypeArguments='{type1},{type2}' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{{DynamicProvider ProvidedValue=DateTime}}' />
"));
        }

        [Fact]
        public void Dynamic_Setter_For_Public_Property_Should_Be_Shared()
        {
            var sharedSetters = _compiler.CreateTypeBuilder("PublicSharedSetters", true);

            _compiler.IlCompiler.DynamicSetterContainerProvider =
                new DefaultXamlDynamicSetterContainerProvider(sharedSetters.XamlTypeBuilder);

            var (create, _) = _compiler.Compile(@"
<DynamicSettersClass 
    x:TypeArguments='sys:Int32,sys:String' 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{DynamicProvider ProvidedValue=String}' />
");

            create(null);

            var dynamicSetterMethod = sharedSetters.RuntimeType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.ToString() == "Void <>XamlDynamicSetter_1(XamlParserTests.DynamicSettersClass`2[System.Int32,System.String], System.Object)");

            Assert.NotNull(dynamicSetterMethod);
            Assert.True(dynamicSetterMethod.IsPublic);
        }

#if CECIL

        [Theory]
        [InlineData(nameof(PrivateDynamicSettersClass))]
        [InlineData(nameof(ProtectedDynamicSettersClass))]
        public void Dynamic_Setter_For_Non_Public_Property_Should_Be_Private(string className)
        {
            var typeSystem = (CecilTypeSystem)Configuration.TypeSystem;

            var xamlAssembly = typeSystem.FindAssembly(typeof(DynamicSettersTests).Assembly.GetName().Name);
            Assert.NotNull(xamlAssembly);

            var assemblyDefinition = typeSystem.GetAssembly(xamlAssembly);
            _compiler.CecilAssembly = assemblyDefinition;

            var sharedSetters = _compiler.CreateTypeBuilder("PublicSharedSetters", true);
            _compiler.IlCompiler.DynamicSetterContainerProvider =
                new DefaultXamlDynamicSetterContainerProvider(sharedSetters.XamlTypeBuilder);

            var rootTypeDefinition = assemblyDefinition.MainModule.GetType($"XamlParserTests.{className}");
            Assert.NotNull(rootTypeDefinition);

            var rootTypeBuilder = new RuntimeTypeBuilder(
                typeSystem.CreateTypeBuilder(rootTypeDefinition),
                () => _compiler.GetRuntimeType(rootTypeDefinition.FullName));

            var (create, _) = _compiler.Compile($@"
<{className} 
    xmlns='clr-namespace:XamlParserTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:sys='clr-namespace:System;assembly=netstandard'
    Value='{{DynamicProvider ProvidedValue=String}}' />
", parsedTypeBuilder: rootTypeBuilder);

            var result = create(null);

            Assert.DoesNotContain(
                sharedSetters.RuntimeType.GetMethods(),
                m => m.ToString()!.Contains("XamlDynamicSetter"));

            var dynamicSetterMethod = result.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.ToString() == $"Void <>XamlDynamicSetter_1(XamlParserTests.{className}, System.Object)");

            Assert.NotNull(dynamicSetterMethod);
            Assert.True(dynamicSetterMethod.IsPrivate);
        }

#endif

        /// <summary>
        /// A transformer that will set a property value either on <see cref="ISpecialHandling{T1,T2}.Value"/>
        /// or <see cref="ISpecialHandling{T1,T2}.SpecialHandler"/> depending on the value's type.
        /// </summary>
        private sealed class SpecialTransformer : IXamlAstTransformer
        {
            public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
            {
                if (node is XamlAstClrProperty
                    {
                        Name: nameof(ISpecialHandling<object, object>.Value),
                        DeclaringType.Namespace: "XamlParserTests"
                    } property
                    && property.DeclaringType.Interfaces.Any(t => t.Name == $"{nameof(ISpecialHandling<object, object>)}`2"))
                {
                    var getSpecialHandlerMethod = property.DeclaringType.Methods
                        .First(m => m.Name == "get_" + nameof(ISpecialHandling<object, object>.SpecialHandler));

                    var handleMethod = getSpecialHandlerMethod.ReturnType.Methods
                        .First(m => m.Name == nameof(SpecialHandler<object>.Handle));

                    return new SpecialProperty(property, getSpecialHandlerMethod, handleMethod);
                }

                return node;
            }
        }

        private sealed class SpecialProperty : XamlAstClrProperty
        {
            public SpecialProperty(
                XamlAstClrProperty original,
                IXamlMethod getSpecialHandlerMethod,
                IXamlMethod handleMethod)
                : base(original, original.Name, original.DeclaringType, original.Getter, original.Setters)
                => Setters.Add(new SpecialHandlerPropertySetter(
                    handleMethod.DeclaringType,
                    handleMethod.Parameters[0],
                    getSpecialHandlerMethod,
                    handleMethod));
        }

        private sealed class SpecialHandlerPropertySetter : IXamlEmitablePropertySetter<IXamlILEmitter>
        {
            private readonly IXamlMethod _getSpecialHandlerMethod;
            private readonly IXamlMethod _handleMethod;

            public SpecialHandlerPropertySetter(
                IXamlType targetType,
                IXamlType propertyType,
                IXamlMethod getSpecialHandlerMethod,
                IXamlMethod handleMethod)
            {
                TargetType = targetType;
                Parameters = new[] { propertyType };
                _getSpecialHandlerMethod = getSpecialHandlerMethod;
                _handleMethod = handleMethod;

                var allowNull = propertyType.AcceptsNull();
                BinderParameters = new PropertySetterBinderParameters
                {
                    AllowMultiple = false,
                    AllowXNull = allowNull,
                    AllowRuntimeNull = allowNull
                };
            }

            public IXamlType TargetType { get; }

            public PropertySetterBinderParameters BinderParameters { get; }

            public IReadOnlyList<IXamlType> Parameters { get; }

            public void Emit(IXamlILEmitter emitter)
            {
                using var local = emitter.LocalsPool.GetLocal(Parameters[0]);

                // C#: target.SpecialHandler.Handle(value)
                emitter
                    .Stloc(local.Local)
                    .EmitCall(_getSpecialHandlerMethod)
                    .Ldloc(local.Local)
                    .EmitCall(_handleMethod);
            }
        }
    }
}
