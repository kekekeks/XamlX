using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlContext
    {
        public IXamlIlField RootObjectField { get; set; }
        public IXamlIlType ContextType { get; set; }
        /// <summary>
        /// Ctor expects IServiceProvider
        /// </summary>
        public IXamlIlConstructor Constructor { get; set; }
        
        public static XamlIlContext GenerateContextClass(IXamlIlTypeBuilder builder, 
            IXamlIlTypeSystem typeSystem, XamlIlLanguageTypeMappings mappings,
            IXamlIlType rootType)
        {
            var rootObjectField = builder.DefineField(rootType, "RootObject", true, false);
            var so = typeSystem.GetType("System.Object");
            var systemType = typeSystem.GetType("System.Type");
            var runtimeType = typeSystem.GetType("System.RuntimeTypeHandle");

            var ownServices = new List<IXamlIlType>();
            if (mappings.RootObjectProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.RootObjectProvider);
                var getRootObject = builder.DefineMethod(so, new IXamlIlType[0],
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
            
            
            return new XamlIlContext
            {
                Constructor = ctor,
                RootObjectField = rootObjectField,
                ContextType = builder.CreateType()
            };
        }
    }
}