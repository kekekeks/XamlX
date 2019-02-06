using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlContext
    {
        public IXamlField RootObjectField { get; set; }
        public IXamlType ContextType { get; set; }
        /// <summary>
        /// Ctor expects IServiceProvider
        /// </summary>
        public IXamlConstructor Constructor { get; set; }
        
        public static XamlContext GenerateContextClass(IXamlTypeBuilder builder, 
            IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings,
            IXamlType rootType)
        {
            var rootObjectField = builder.DefineField(rootType, "RootObject", true, false);
            var so = typeSystem.GetType("System.Object");
            var systemType = typeSystem.GetType("System.Type");
            var runtimeType = typeSystem.GetType("System.RuntimeTypeHandle");

            var ownServices = new List<IXamlType>();
            if (mappings.RootObjectProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.RootObjectProvider);
                var getRootObject = builder.DefineMethod(so, new IXamlType[0],
                    "get_RootObject", true, false, true);
                builder.DefineProperty(so, "RootObject",null, getRootObject);
                getRootObject.Generator
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Ldfld, rootObjectField)
                    .Emit(OpCodes.Ret);
                ownServices.Add(mappings.RootObjectProvider);
            }
            
            var spField = builder.DefineField(mappings.ServiceProvider, "_sp", false, false);
            builder.AddInterfaceImplementation(mappings.ServiceProvider);
            var getServiceMethod = builder.DefineMethod(so,
                new[] {systemType},
                "GetService", true, false, true);

            if (ownServices.Count != 0)
            {
                var compare = systemType.FindMethod("Equals", typeSystem.GetType("System.Boolean"),
                    false, systemType);
                var fromHandle = systemType.Methods.First(m => m.Name == "GetTypeFromHandle");
                
                for (var c = 0; c < ownServices.Count; c++)
                {
                    
                    var next = getServiceMethod.Generator.DefineLabel();
                    getServiceMethod.Generator
                        .Emit(OpCodes.Ldtoken, ownServices[c])
                        .Emit(OpCodes.Call, fromHandle)
                        .Emit(OpCodes.Ldarg_1)
                        .Emit(OpCodes.Callvirt, compare)
                        .Emit(OpCodes.Brfalse, next)
                        .Emit(OpCodes.Ldarg_0)
                        .Emit(OpCodes.Ret)
                        .MarkLabel(next);
                }
                
            }

            getServiceMethod.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, spField)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Callvirt, mappings.ServiceProvider.FindMethod("GetService", so, false, systemType))
                .Emit(OpCodes.Ret);


            var ctor = builder.DefineConstructor(mappings.ServiceProvider);
            ctor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, spField)
                .Emit(OpCodes.Ret);
            
            
            return new XamlContext
            {
                Constructor = ctor,
                RootObjectField = rootObjectField,
                ContextType = builder.CreateType()
            };
        }
    }
}