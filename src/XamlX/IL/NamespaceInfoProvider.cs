using System;
using System.Collections.Generic;
using System.Linq;
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
        public static IXamlField EmitNamespaceInfoProvider(TransformerConfiguration configuration,
            IXamlTypeBuilder<IXamlILEmitter> typeBuilder, XamlDocument document)
        {
            var iface = configuration.TypeMappings.XmlNamespaceInfoProvider;
            typeBuilder.AddInterfaceImplementation(iface);
            var method = iface.FindMethod(m => m.Name == "get_XmlNamespaces");
            var instField = typeBuilder.DefineField(method.ReturnType, "_services", false, false);
            var singletonField = typeBuilder.DefineField(iface, "Singleton", true, true);

            var impl = typeBuilder.DefineMethod(method.ReturnType, null, method.Name, true, false, true);
            typeBuilder.DefineProperty(method.ReturnType, "XmlNamespaces", null, impl);
            impl.Generator
                .LdThisFld(instField)
                .Ret();

            var infoType = method.ReturnType.GenericArguments[1].GenericArguments[0];
            
            var ctor = typeBuilder.DefineConstructor(false);
            var listType = configuration.TypeSystem.FindType("System.Collections.Generic.List`1")
                .MakeGenericType(infoType);
            var listInterfaceType = configuration.TypeSystem.FindType("System.Collections.Generic.IReadOnlyList`1")
                .MakeGenericType(infoType);
            var listAdd = listType.FindMethod("Add", configuration.WellKnownTypes.Void, true, infoType);
            
            var dictionaryType = configuration.TypeSystem.FindType("System.Collections.Generic.Dictionary`2")
                .MakeGenericType(configuration.WellKnownTypes.String, listInterfaceType);
            var dictionaryAdd = dictionaryType.FindMethod("Add", configuration.WellKnownTypes.Void, true,
                configuration.WellKnownTypes.String, listInterfaceType);
            
            var dicLocal = ctor.Generator.DefineLocal(dictionaryType);
            var listLocal = ctor.Generator.DefineLocal(listType);

            ctor.Generator
                .Ldarg_0()
                .Emit(OpCodes.Call, configuration.WellKnownTypes.Object.FindConstructor())
                .Emit(OpCodes.Newobj, dictionaryType.FindConstructor())
                .Stloc(dicLocal)
                .Ldarg_0()
                .Ldloc(dicLocal)
                .Stfld(instField);
            
            foreach (var alias in document.NamespaceAliases)
            {
                ctor.Generator
                    .Newobj(listType.FindConstructor(new List<IXamlType>()))
                    .Stloc(listLocal);

                var resolved = Transform.NamespaceInfoHelper.TryResolve(configuration, alias.Value);
                if (resolved != null)
                {
                    foreach (var rns in resolved)
                    {
                        ctor.Generator
                            .Ldloc(listLocal)
                            .Newobj(infoType.FindConstructor());
                        if (rns.ClrNamespace != null)
                            ctor.Generator
                                .Dup()
                                .Ldstr(rns.ClrNamespace)
                                .EmitCall(infoType.FindMethod(m => m.Name == "set_ClrNamespace"));

                        var asmName = rns.AssemblyName ?? rns.Assembly?.Name;
                        if (asmName != null)
                            ctor.Generator
                                .Dup()
                                .Ldstr(asmName)
                                .EmitCall(infoType.FindMethod(m => m.Name == "set_ClrAssemblyName"));

                        ctor.Generator.EmitCall(listAdd);
                    }
                }

                ctor.Generator
                    .Ldloc(dicLocal)
                    .Ldstr(alias.Key)
                    .Ldloc(listLocal)
                    .EmitCall(dictionaryAdd, true);
            }

            ctor.Generator.Ret();

            var sctor = typeBuilder.DefineConstructor(true);
            sctor.Generator
                .Newobj(ctor)
                .Stsfld(singletonField)
                .Ret();

            return singletonField;
            //return typeBuilder.CreateType().Fields.First(f => f.Name == "Singleton");
        }
    }
}
