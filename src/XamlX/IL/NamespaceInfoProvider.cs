using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    static class NamespaceInfoProvider
    {
        public static IXamlField EmitNamespaceInfoProvider(
            TransformerConfiguration configuration,
            IXamlTypeBuilder<IXamlILEmitter> typeBuilder,
            XamlDocument document)
        {
            var nsInfoProviderType = configuration.TypeMappings.XmlNamespaceInfoProvider;
            var getNamespacesInterfaceMethod = nsInfoProviderType.FindMethod(m => m.Name == "get_XmlNamespaces");
            var roDictionaryType = getNamespacesInterfaceMethod.ReturnType;
            var nsInfoType = roDictionaryType.GenericArguments[1].GenericArguments[0];

            typeBuilder.AddInterfaceImplementation(nsInfoProviderType);
            var namespacesField = typeBuilder.DefineField(roDictionaryType, "_xmlNamespaces", false, false);
            var singletonField = typeBuilder.DefineField(nsInfoProviderType, "Singleton", true, true);

            IXamlMethod EmitCreateNamespaceInfoMethod()
            {
                // private static XamlXmlNamespaceInfoV1 CreateNamespaceInfo(string arg0, string arg1)
                var method = typeBuilder.DefineMethod(
                    nsInfoType,
                    new[] { configuration.WellKnownTypes.String, configuration.WellKnownTypes.String },
                    "CreateNamespaceInfo",
                    false, true, false);

                // return new XamlXmlNamespaceInfoV1() { ClrNamespace = arg0, ClrAssemblyName = arg1 }
                method.Generator
                    .Newobj(nsInfoType.FindConstructor())
                    .Dup()
                    .Ldarg_0()
                    .EmitCall(nsInfoType.FindMethod(m => m.Name == "set_ClrNamespace"))
                    .Dup()
                    .Ldarg(1)
                    .EmitCall(nsInfoType.FindMethod(m => m.Name == "set_ClrAssemblyName"))
                    .Ret();

                return method;
            }

            var createNamespaceInfoMethod = EmitCreateNamespaceInfoMethod();

            IXamlMethod EmitCreateNamespacesMethod()
            {
                // C#: private static IReadOnlyDictionary<string, IReadOnlyList<string>> CreateNamespaces()
                var method = typeBuilder.DefineMethod(
                    roDictionaryType,
                    null,
                    "CreateNamespaces",
                    false, true, false);

                var roListType = configuration.TypeSystem.FindType("System.Collections.Generic.IReadOnlyList`1")
                    .MakeGenericType(nsInfoType);

                var dictionaryType = configuration.TypeSystem.FindType("System.Collections.Generic.Dictionary`2")
                    .MakeGenericType(configuration.WellKnownTypes.String, roListType);

                var dictionaryCtor = dictionaryType.FindConstructor(new List<IXamlType> { configuration.WellKnownTypes.Int32 });

                var dictionaryAddMethod = dictionaryType.FindMethod(
                    "Add",
                    configuration.WellKnownTypes.Void, true, configuration.WellKnownTypes.String, roListType);

                var codeGen = method.Generator;
                var dictionaryLocal = codeGen.DefineLocal(dictionaryType);

                // C#: var dic = new Dictionary<string, IReadOnlyList<string>>(`capacity`);
                codeGen
                    .Ldc_I4(document.NamespaceAliases.Count)
                    .Newobj(dictionaryCtor)
                    .Stloc(dictionaryLocal);

                foreach (var alias in document.NamespaceAliases)
                {
                    codeGen
                        .Ldloc(dictionaryLocal)
                        .Ldstr(alias.Key);

                    var resolveResults = NamespaceInfoHelper.TryResolve(configuration, alias.Value) ?? new();

                    // C#: var array = new XamlXmlNamespaceInfoV1[`count`];
                    codeGen
                        .Ldc_I4(resolveResults.Count)
                        .Newarr(nsInfoType);

                    for (int i = 0; i < resolveResults.Count; ++i)
                    {
                        var resolveResult = resolveResults[i];

                        // C#: array[`i`] = CreateNamespace(`namespace`, `assemblyName`);
                        codeGen
                            .Dup()
                            .Ldc_I4(i)
                            .Ldstr(resolveResult.ClrNamespace)
                            .Ldstr(resolveResult.AssemblyName ?? resolveResult.Assembly?.Name)
                            .EmitCall(createNamespaceInfoMethod)
                            .Stelem_ref();
                    }

                    // C#: dic.Add(`alias`, array);
                    codeGen.EmitCall(dictionaryAddMethod, true);
                }

                // C#: return dic;
                codeGen
                    .Ldloc(dictionaryLocal)
                    .Ret();

                return method;
            }

            var createNamespacesMethod = EmitCreateNamespacesMethod();

            void EmitNamespacesProperty()
            {
                // C#: private IReadOnlyDictionary<string, IReadOnlyList<string>> get_XmlNamespaces()
                var method = typeBuilder.DefineMethod(
                    roDictionaryType,
                    null,
                    getNamespacesInterfaceMethod.Name,
                    true, false, true);

                var hasValueLabel = method.Generator.DefineLabel();

                method.Generator
                    // C#: if (this._xmlNamespaces == null)
                    .Ldarg_0()
                    .Ldfld(namespacesField)
                    .Brtrue(hasValueLabel)
                    // C#:     this._xmlNamespaces = CreateNamespaces();
                    .Ldarg_0()
                    .EmitCall(createNamespacesMethod)
                    .Stfld(namespacesField)
                    // C#: return this._xmlNamespaces
                    .MarkLabel(hasValueLabel)
                    .Ldarg_0()
                    .Ldfld(namespacesField)
                    .Ret();

                typeBuilder.DefineProperty(roDictionaryType, "XmlNamespaces", null, method);
            }

            EmitNamespacesProperty();

            IXamlConstructor EmitConstructor()
            {
                var ctor = typeBuilder.DefineConstructor(false);

                // C#: base()
                ctor.Generator
                    .Ldarg_0()
                    .Emit(OpCodes.Call, configuration.WellKnownTypes.Object.FindConstructor())
                    .Ret();

                return ctor;
            }

            var ctor = EmitConstructor();

            void EmitStaticConstructor()
            {
                var cctor = typeBuilder.DefineConstructor(true);

                // C#: _singleton = new NamespaceInfoProvider();
                cctor.Generator
                    .Newobj(ctor)
                    .Stsfld(singletonField)
                    .Ret();
            }

            EmitStaticConstructor();

            return singletonField;
        }

    }
}
