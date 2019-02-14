using System.Linq;
using Mono.Cecil;
using XamlX.TypeSystem;
using Xunit;

namespace XamlParserTests
{
    interface IGenericInterface<T>
    {
        
    }
    
    public class GenericBaseType<T>{}
    
    public class GenericType<T> : GenericBaseType<T>, IGenericInterface<T>
    {
        public T Field;
        public T Property { get; set; }
        public T SomeMethod(T x) => x;
    }

    public class CecilTestType
    {
        public GenericType<string> GenericStringField;
    }
    
    public class CecilTests : CompilerTestBase
    {
        [Fact]
        public void Generic_Types_Are_Correctly_Handled()
        {
            var wut = Configuration.TypeSystem.GetType("XamlParserTests.CecilTestType");
            var f = wut.Fields.First(x => x.Name == nameof(CecilTestType.GenericStringField));
            Assert.Equal("XamlParserTests.GenericType`1<System.String>", f.FieldType.FullName);
            Assert.Equal("XamlParserTests.GenericBaseType`1<System.String>", f.FieldType.BaseType.FullName);
            var iface = f.FieldType.Interfaces.First(x => x.Name == "IGenericInterface`1");
            Assert.Equal("XamlParserTests.IGenericInterface`1<System.String>", iface.FullName);

            var m = f.FieldType.Methods.First(x => x.Name == "SomeMethod");
            Assert.Equal("System.String", m.ReturnType.FullName);
            Assert.Equal("System.String", m.Parameters[0].FullName);

            var gf = f.FieldType.Fields.First(x => x.Name == "Field");
            Assert.Equal("System.String", gf.FieldType.FullName);

            var p = f.FieldType.Properties.First(x => x.Name == "Property");
            Assert.Equal("System.String", p.PropertyType.FullName);
            Assert.Equal("System.String", p.Getter.ReturnType.FullName);
            Assert.Equal("System.String", p.Setter.Parameters[0].FullName);


        }
    }
}