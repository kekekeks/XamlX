using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using XamlX.IL;

namespace XamlX.TypeSystem
{
    partial class CecilTypeSystem
    {
        class CecilTypeBuilder : CecilType, IXamlTypeBuilder<IXamlILEmitter>
        {
            private CecilTypeResolveContext _builderTypeResolveContext;

            public CecilTypeBuilder(CecilTypeResolveContext parentTypeResolveContext, CecilAssembly? assembly, TypeDefinition definition)
                : base(parentTypeResolveContext, assembly, definition)
            {
                _builderTypeResolveContext = parentTypeResolveContext.Nested(definition);
            }

            TypeReference GetReference(IXamlType type) =>
                Definition.Module.ImportReference(((ITypeReference) type).Reference);
            
            public IXamlField DefineField(IXamlType type, string name, XamlVisibility visibility, bool isStatic)
            {
                var r = GetReference(type);
                var attrs = default(FieldAttributes);

                attrs |= visibility switch
                {
                    XamlVisibility.Public => FieldAttributes.Public,
                    XamlVisibility.Assembly => FieldAttributes.Assembly,
                    XamlVisibility.Private => FieldAttributes.Private,
                    _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
                };

                if (isStatic)
                    attrs |= FieldAttributes.Static;

                var def = new FieldDefinition(name, attrs, r);
                Definition.Fields.Add(def);
                var rv = new CecilField(_builderTypeResolveContext, def);
                ((List<CecilField>)Fields).Add(rv);
                return rv;
            }

            public void AddInterfaceImplementation(IXamlType type)
            {
                Definition.Interfaces.Add(new InterfaceImplementation(GetReference(type)));
                _interfaces = null;
            }

            public IXamlMethodBuilder<IXamlILEmitter> DefineMethod(IXamlType returnType, IEnumerable<IXamlType> args, string name,
                XamlVisibility visibility, bool isStatic, bool isInterfaceImpl, IXamlMethod? overrideMethod = null)
            {
                var attrs = default(MethodAttributes);

                attrs |= visibility switch
                {
                    XamlVisibility.Public => MethodAttributes.Public,
                    XamlVisibility.Assembly => MethodAttributes.Assembly,
                    XamlVisibility.Private => MethodAttributes.Private,
                    _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
                };

                if (isStatic)
                    attrs |= MethodAttributes.Static;

                if (isInterfaceImpl)
                    attrs |= MethodAttributes.NewSlot | MethodAttributes.Virtual;
                               
                var def = new MethodDefinition(name, attrs, GetReference(returnType));

                foreach (var arg in args)
                    def.Parameters.Add(new ParameterDefinition(GetReference(arg)));

                if (overrideMethod != null)
                    def.Overrides.Add(Definition.Module.ImportReference(((CecilMethod) overrideMethod).Reference));
                def.Body.InitLocals = true;
                Definition.Methods.Add(def);
                TypeSystem.AddCompilerGeneratedAttribute(def);
                var rv = new CecilMethod(_builderTypeResolveContext, def);
                ((List<CecilMethod>)Methods).Add(rv);
                return rv;
            }
            
            public IXamlProperty DefineProperty(IXamlType propertyType, string name, IXamlMethod? setter, IXamlMethod? getter)
            {
                var s = (CecilMethod?) setter;
                var g = (CecilMethod?) getter;
                var def = new PropertyDefinition(name, PropertyAttributes.None,
                    GetReference(propertyType));
                def.SetMethod = s?.Definition;
                def.GetMethod = g?.Definition;
                Definition.Properties.Add(def);
                var rv = new CecilProperty(_builderTypeResolveContext, def);
                ((List<CecilProperty>)Properties).Add(rv);
                return rv;
            }

            public IXamlConstructorBuilder<IXamlILEmitter> DefineConstructor(bool isStatic, params IXamlType[] args)
            {
                var attrs = MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

                if (isStatic)
                    attrs |= MethodAttributes.Static;
                else
                    attrs |= MethodAttributes.Public;

                var def = new MethodDefinition(isStatic ? ".cctor" : ".ctor", attrs,
                    Definition.Module.TypeSystem.Void);

                foreach (var arg in args)
                    def.Parameters.Add(new ParameterDefinition(GetReference(arg)));

                def.Body.InitLocals = true;
                Definition.Methods.Add(def);
                var rv = new CecilConstructor(_builderTypeResolveContext, def);
                ((List<CecilConstructor>)Constructors).Add(rv);
                return rv;
            }

            public IXamlType CreateType() => this;

            public IXamlTypeBuilder<IXamlILEmitter> DefineSubType(IXamlType baseType, string name, XamlVisibility visibility)
            {
                var attrs = TypeAttributes.Class;

                attrs |= visibility switch
                {
                    XamlVisibility.Public => TypeAttributes.NestedPublic,
                    XamlVisibility.Assembly => TypeAttributes.NestedAssembly,
                    XamlVisibility.Private => TypeAttributes.NestedPrivate,
                    _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
                };

                var td = new TypeDefinition("", name, attrs, GetReference(baseType));

                Definition.NestedTypes.Add(td);
                TypeSystem.AddCompilerGeneratedAttribute(td);
                return new CecilTypeBuilder(_builderTypeResolveContext, (CecilAssembly?)Assembly, td);
            }

            public IXamlTypeBuilder<IXamlILEmitter> DefineDelegateSubType(string name, XamlVisibility visibility,
                IXamlType returnType, IEnumerable<IXamlType> parameterTypes)
            {
                var attrs = TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoLayout;

                attrs |= visibility switch
                {
                    XamlVisibility.Public => TypeAttributes.NestedPublic,
                    XamlVisibility.Assembly => TypeAttributes.NestedAssembly,
                    XamlVisibility.Private => TypeAttributes.NestedPrivate,
                    _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
                };

                var builder = new TypeDefinition("", name, attrs, GetReference(TypeSystem.GetType("System.MulticastDelegate")));

                Definition.NestedTypes.Add(builder);
                TypeSystem.AddCompilerGeneratedAttribute(builder);

                var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, GetReference(returnType));
                ctor.ImplAttributes = MethodImplAttributes.Managed | MethodImplAttributes.Runtime;
                ctor.Parameters.Add(new ParameterDefinition(GetReference(TypeSystem.GetType("System.Object"))));
                ctor.Parameters.Add(new ParameterDefinition(GetReference(TypeSystem.GetType("System.IntPtr"))));

                builder.Methods.Add(ctor);

                var invoke = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig, GetReference(returnType));
                invoke.ImplAttributes = MethodImplAttributes.Managed | MethodImplAttributes.Runtime;

                foreach (var param in parameterTypes)
                {
                    invoke.Parameters.Add(new ParameterDefinition(GetReference(param)));
                }

                return new CecilTypeBuilder(_builderTypeResolveContext, (CecilAssembly?)Assembly, builder);
            }

            public void DefineGenericParameters(IReadOnlyList<KeyValuePair<string, XamlGenericParameterConstraint>> args)
            {
                foreach (var arg in args)
                {
                    var gp = new GenericParameter(arg.Key, Definition);
                    // TODO: for some reason types can't be instantiated properly
                    /*if (arg.Value.IsClass)
                        gp.Attributes = GenericParameterAttributes.NotNullableValueTypeConstraint;*/
                    Definition.GenericParameters.Add(gp);
                }

                Definition.Name = Name + "`" + args.Count;
                Reference.Name = Definition.Name;
                var selfReference = Definition.MakeGenericInstanceType(Definition.GenericParameters.Cast<TypeReference>()
                    .ToArray());
                _builderTypeResolveContext = _builderTypeResolveContext.Nested(selfReference);
            }
        }
    }
}
