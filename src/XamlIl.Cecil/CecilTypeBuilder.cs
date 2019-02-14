using System.Collections.Generic;
using Mono.Cecil;

namespace XamlIl.TypeSystem
{
    public partial class CecilTypeSystem
    {
        class CecilTypeBuilder : CecilType, IXamlIlTypeBuilder
        {
            public CecilTypeBuilder(CecilTypeSystem typeSystem, CecilAssembly assembly, TypeDefinition definition) 
                : base(typeSystem, assembly, definition)
            {
            }

            TypeReference GetReference(IXamlIlType type) =>
                Definition.Module.ImportReference(((CecilType) type).Reference);
            
            public IXamlIlField DefineField(IXamlIlType type, string name, bool isPublic, bool isStatic)
            {
                var r = GetReference(type);
                var attrs = default(FieldAttributes);
                if (isPublic)
                    attrs |= FieldAttributes.Public;
                if (isStatic)
                    attrs |= FieldAttributes.Static;

                var def = new FieldDefinition(name, attrs, r);
                Definition.Fields.Add(def);
                var rv = new CecilField(TypeSystem, def, Definition);
                ((List<CecilField>)Fields).Add(rv);
                return rv;
            }

            public void AddInterfaceImplementation(IXamlIlType type)
            {
                Definition.Interfaces.Add(new InterfaceImplementation(GetReference(type)));
                _interfaces = null;
            }

            public IXamlIlMethodBuilder DefineMethod(IXamlIlType returnType, IEnumerable<IXamlIlType> args, string name, bool isPublic, bool isStatic,
                bool isInterfaceImpl, IXamlIlMethod overrideMethod = null)
            {
                var attrs = default(MethodAttributes);
                if (isPublic)
                    attrs |= MethodAttributes.Public;
                if (isStatic)
                    attrs |= MethodAttributes.Static;

                if (isInterfaceImpl)
                    attrs |= MethodAttributes.NewSlot | MethodAttributes.Virtual;
                               
                var def = new MethodDefinition(name, attrs, GetReference(returnType));
                if(args!=null)
                    foreach (var a in args)
                        def.Parameters.Add(new ParameterDefinition(GetReference(a)));
                if (overrideMethod != null)
                    def.Overrides.Add(Definition.Module.ImportReference(((CecilMethod) overrideMethod).Reference));
                
                Definition.Methods.Add(def);
                var rv = new CecilMethod(TypeSystem, def, Definition);
                ((List<CecilMethod>)Methods).Add(rv);
                return rv;
            }
            
            public IXamlIlProperty DefineProperty(IXamlIlType propertyType, string name, IXamlIlMethod setter, IXamlIlMethod getter)
            {
                var s = (CecilMethod) setter;
                var g = (CecilMethod) getter;
                var def = new PropertyDefinition(name, PropertyAttributes.None,
                    GetReference(propertyType));
                def.SetMethod = s?.Definition;
                def.GetMethod = g?.Definition;
                Definition.Properties.Add(def);
                var rv = new CecilProperty(TypeSystem, def, Definition);
                ((List<CecilProperty>)Properties).Add(rv);
                return rv;
            }

            public IXamlIlConstructorBuilder DefineConstructor(bool isStatic, params IXamlIlType[] args)
            {
                var attrs = MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

                if (isStatic)
                    attrs |= MethodAttributes.Static;
                else
                    attrs |= MethodAttributes.Public;

                var def = new MethodDefinition(isStatic ? ".cctor" : ".ctor", attrs,
                    Definition.Module.TypeSystem.Void);
                if(args!=null)
                    foreach (var a in args)
                        def.Parameters.Add(new ParameterDefinition(GetReference(a)));
                
                Definition.Methods.Add(def);
                var rv = new CecilConstructor(TypeSystem, def, Definition);
                ((List<CecilConstructor>)Constructors).Add(rv);
                return rv;
            }

            public IXamlIlType CreateType() => this;

            public IXamlIlTypeBuilder DefineSubType(IXamlIlType baseType, string name, bool isPublic)
            {
                var td = new TypeDefinition("", name,
                    isPublic ? TypeAttributes.NestedPublic : TypeAttributes.NestedPrivate, GetReference(baseType));
                Definition.NestedTypes.Add(td);
                return new CecilTypeBuilder(TypeSystem, (CecilAssembly) Assembly, td);
            }
        }

    }
}