using XamlX.TypeSystem;
using Xunit;

namespace TypeSystemTest;

partial class BaseTest
{
    protected const string NestedTypesNamespace = $"{ModelsNamespace}.{nameof(Models.NestedTyeps)}";
    protected const string PublicNestedTypeName = $"{NestedTypesNamespace}.{nameof(Models.NestedTyeps.PublicNestedTypeContainer)}";
    protected const string InternalNestedTypeName = $"{NestedTypesNamespace}.{nameof(Models.NestedTyeps.InternaNestedTypeContainer)}";
    protected const string PrivateNestedTypeName = $"{NestedTypesNamespace}.{nameof(Models.NestedTyeps.PrivateNestedTypeContainer)}";

    [Theory]
    [InlineData($"{PublicNestedTypeName}+{nameof(Models.NestedTyeps.PublicNestedTypeContainer.PublicNestedType)}", false)]
    [InlineData($"{PublicNestedTypeName}+{nameof(Models.NestedTyeps.PublicNestedTypeContainer.InternalNestedType)}", false)]
    [InlineData($"{PublicNestedTypeName}+PrivateNestedType", true)]
    [InlineData($"{InternalNestedTypeName}+{nameof(Models.NestedTyeps.PublicNestedTypeContainer.PublicNestedType)}", false)]
    [InlineData($"{InternalNestedTypeName}+{nameof(Models.NestedTyeps.PublicNestedTypeContainer.InternalNestedType)}", false)]
    [InlineData($"{InternalNestedTypeName}+PrivateNestedType", true)]
    [InlineData($"{PrivateNestedTypeName}+NestedPrivate+NestedPublic", true)]
    public void Should_Handled_Nested_Type(string typeName, bool throwXamlTypeSystemException)
    {
        System.Exception? exception = null;
        try
        {
            var wut = TypeSystem.GetType(typeName);
            Assert.NotNull(wut);
        }
        catch (System.Exception ex)
        {
            exception = ex;
        }

        if (throwXamlTypeSystemException)
        {
            Assert.NotNull(exception);
            Assert.IsType<XamlX.XamlTypeSystemException>(exception);
        }
    }
}
