using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using XamlParserTests;
using XamlX.TypeSystem;
using Xunit;

namespace CecilTests
{
    public class CecilHelpersTest : CompilerTestBase
    {
        [Fact]
        public void Different_Generic_Base_Types_With_Same_GenericArgs_Are_Not_Equal()
        {
            var type1 = MakeGenericType("System.Collections.Generic.ISet`1", "System.String");
            var type2 = MakeGenericType("System.IComparable`1", "System.String");
            Assert.False(type1.Equals(type2));
            Assert.False(type2.Equals(type1));
        }

        [Fact]
        public void Same_Base_Type_With_Different_GenericArgs_Are_Not_Equal()
        {
            var type1 = MakeGenericType("System.Collections.Generic.ISet`1", "System.String");
            var type2 = MakeGenericType("System.Collections.Generic.ISet`1", "System.Char");
            Assert.False(type1.Equals(type2));
            Assert.False(type2.Equals(type1));
        }

        [Fact]
        public void Types_With_Different_GenericArgs_Counts_Are_Not_Equal()
        {
            var type1 = MakeGenericType("System.Tuple`1", "System.String");
            var type2 = MakeGenericType("System.Tuple`2", "System.String", "System.String");
            Assert.False(type1.Equals(type2));
            Assert.False(type2.Equals(type1));
        }

        [Fact]
        public void Different_Reference_Types_Are_NotEqual()
        {
            var type1 = Configuration.TypeSystem.GetType("System.String");
            var type2 = Configuration.TypeSystem.GetType("System.Text.StringBuilder");
            Assert.False(type1.Equals(type2));
            Assert.False(type2.Equals(type1));
        }

        [Fact]
        public void Same_Reference_Types_Are_Equal()
        {
            var type1 = Configuration.TypeSystem.GetType("System.String");
            var type2 = Configuration.TypeSystem.GetType("System.String");
            Assert.True(type1.Equals(type2));
            Assert.True(type2.Equals(type1));
        }

        [Fact]
        public void Dictionary_With_Generics_Should_Not_Equal_To_Plain()
        {
            var type1 = MakeGenericType("System.Collections.Generic.Dictionary`2", "System.String", "System.String");
            var type2 = Configuration.TypeSystem.GetType("System.Collections.Generic.Dictionary`2");
            Assert.False(type1.Equals(type2));
            Assert.False(type2.Equals(type1));
        }

        [Fact]
        public void Dictionary_Base_Interface_Should_Be_Completely_Resolved()
        {
            var type1 = MakeGenericType("System.Collections.Generic.Dictionary`2", "System.String", "System.String")
                .GetAllInterfaces().First(i => i.Name.StartsWith("ICollection`"));
            var type2 = Configuration.TypeSystem.GetType("System.Collections.Generic.ICollection`1")
                .MakeGenericType(Configuration.TypeSystem.GetType("System.Collections.Generic.KeyValuePair`2")
                    .MakeGenericType(Configuration.TypeSystem.GetType("System.String"), Configuration.TypeSystem.GetType("System.String")));
            Assert.True(type1.Equals(type2));
            Assert.True(type2.Equals(type1));
        }

        [Fact]
        public void Dictionary_ICollection_Methods_Should_Be_Completely_Resolved()
        {
            var type = MakeGenericType("System.Collections.Generic.Dictionary`2", "System.String", "System.String");

            var collectionMethod = type.GetMethod(m => m.Name == "Add" && m.Parameters.Count == 1);
            var inputParam = collectionMethod.Parameters[0];
            var expectedParamType = Configuration.TypeSystem.GetType("System.Collections.Generic.KeyValuePair`2")
                .MakeGenericType(Configuration.TypeSystem.GetType("System.String"), Configuration.TypeSystem.GetType("System.String"));
            Assert.True(expectedParamType.Equals(inputParam));
            Assert.True(inputParam.Equals(expectedParamType));
        }

        private IXamlType MakeGenericType(string baseType, params string[] genericArgs)
        {
            return Configuration.TypeSystem.GetType(baseType)
                .MakeGenericType(genericArgs.Select(t => Configuration.TypeSystem.GetType(t)).ToArray());
        }

    }
}