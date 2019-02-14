using Mono.Cecil;

namespace XamlIl.TypeSystem
{
    public partial class CecilTypeSystem
    {
        public class CecilField : IXamlIlField
        {
            private readonly FieldDefinition _def;
            public CecilTypeSystem TypeSystem { get; }
            public FieldReference Field { get; }

            public CecilField(CecilTypeSystem typeSystem, FieldDefinition def, TypeReference declaringType)
            {
                TypeSystem = typeSystem;
                _def = def;
                Field = new FieldReference(def.Name, def.FieldType.TransformGeneric(declaringType), declaringType);
            }

            public bool Equals(IXamlIlField other) => other is CecilField cf && cf.Field == Field;

            public string Name => Field.Name;
            private IXamlIlType _type;
            public IXamlIlType FieldType => _type ?? (_type = TypeSystem.Resolve(Field.FieldType));
            public bool IsPublic => _def.IsPublic;
            public bool IsStatic => _def.IsStatic;
            public bool IsLiteral => _def.IsLiteral;
            public object GetLiteralValue()
            {
                if (IsLiteral && _def.HasConstant)
                    return _def.Constant;
                return null;
            }
        }
    }
}