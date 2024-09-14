using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    class RuntimeContext : XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public RuntimeContext(IXamlType definition, IXamlType constructedType,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> mappings,
            string? baseUri, List<IXamlField>? staticProviders)
            : base(definition, constructedType, baseUri, mappings,
            (context, codegen) =>
            {
                if (staticProviders?.Count > 0)
                {
                    var so = codegen.TypeSystem.GetType("System.Object");
                    codegen.Ldc_I4(staticProviders.Count)
                        .Newarr(so);
                    for (var c = 0; c < staticProviders.Count; c++)
                    {
                        codegen
                            .Dup()
                            .Ldc_I4(c)
                            .Ldsfld(staticProviders[c])
                            .Castclass(so)
                            .Stelem_ref();
                    }
                }
                else
                    codegen.Ldnull();

                codegen.Ldstr(baseUri)
                    .Newobj(context.Constructor);
            })
        {

        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlILContextDefinition<TBackendEmitter>
    {
        public IXamlTypeBuilder<TBackendEmitter> TypeBuilder { get; }
        public IXamlConstructorBuilder<TBackendEmitter> ConstructorBuilder { get; }
        public IXamlField? ParentListField { get; }
        public IXamlField ParentServiceProviderField { get; }
        public IXamlField? InnerServiceProviderField { get; }
        public IXamlField? TargetObjectField { get; }
        public IXamlField? TargetPropertyField { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class XamlILContextDefinition : IXamlILContextDefinition<IXamlILEmitter>
    {
        public static IXamlType GenerateContextClass(IXamlTypeBuilder<IXamlILEmitter> builder,
            IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings)
        {
            return new XamlILContextDefinition(builder, typeSystem, mappings, emitMappings).ContextType;

        }

        public IXamlTypeBuilder<IXamlILEmitter> TypeBuilder { get; }
        public IXamlConstructorBuilder<IXamlILEmitter> ConstructorBuilder { get; }
        public IXamlField? ParentListField { get; }
        public IXamlField ParentServiceProviderField { get; }
        public IXamlField? InnerServiceProviderField { get; }
        public IXamlField? TargetObjectField { get; }
        public IXamlField? TargetPropertyField { get; }
        public IXamlType ContextType { get; }
        public List<Action> CreateCallbacks { get; } = new();

        public const int BaseUriArg = 0;
        public const int StaticProvidersArg = 1;
        
        private XamlILContextDefinition(IXamlTypeBuilder<IXamlILEmitter> parentBuilder,
            IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings)
        {
            var so = typeSystem.GetType("System.Object");
            var builder = parentBuilder.DefineSubType(so, "Context", XamlVisibility.Public);
            TypeBuilder = builder;
            
            builder.DefineGenericParameters(new[]
            {
                new KeyValuePair<string, XamlGenericParameterConstraint>("TTarget",
                    new XamlGenericParameterConstraint
                    {
                        IsClass = true
                    })
            });
            var rootObjectField = builder.DefineField(builder.GenericParameters[0], "RootObject", XamlVisibility.Public, false);
            var intermediateRootObjectField = builder.DefineField(so, XamlRuntimeContextDefintion.IntermediateRootObjectFieldName, XamlVisibility.Public, false);
            ParentServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_sp", XamlVisibility.Private, false);
            if (mappings.InnerServiceProviderFactoryMethod != null)
                InnerServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_innerSp", XamlVisibility.Private, false);

            var staticProvidersField = builder.DefineField(typeSystem.GetType("System.Object").MakeArrayType(1),
                "_staticProviders", XamlVisibility.Private, false);
            
            
            var systemType = typeSystem.GetType("System.Type");
            var systemUri = typeSystem.GetType("System.Uri");
            var systemString = typeSystem.GetType("System.String");
            var getServiceInterfaceMethod = mappings.ServiceProvider.GetMethod("GetService", so, false, systemType);

            var ownServices = new List<IXamlType>();
            var ctorCallbacks = new List<Action<IXamlILEmitter>>();
            
            if (mappings.RootObjectProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.RootObjectProvider);
                var rootGen = ImplementInterfacePropertyGetter(builder, mappings.RootObjectProvider, XamlRuntimeContextDefintion.RootObjectFieldName)
                    .Generator;
                var tryParent = rootGen.DefineLabel();
                var fail = rootGen.DefineLabel();
                var parentRootProvider = rootGen.DefineLocal(mappings.RootObjectProvider);
                rootGen
                    // if(RootObject!=null) return RootObject;
                    .LdThisFld(rootObjectField)
                    .Box(rootObjectField.FieldType)
                    .Brfalse(tryParent)
                    .LdThisFld(rootObjectField)
                    .Box(rootObjectField.FieldType)
                    .Ret()
                    // if(_sp == null) goto fail;
                    .MarkLabel(tryParent)
                    .LdThisFld(ParentServiceProviderField)
                    .Brfalse(fail)
                    // parentProv =  (IRootObjectProvider)_sp.GetService(typeof(IRootObjectProvider));
                    .LdThisFld(ParentServiceProviderField)
                    .Ldtype(mappings.RootObjectProvider)
                    .EmitCall(getServiceInterfaceMethod)
                    .Castclass(mappings.RootObjectProvider)
                    .Stloc(parentRootProvider)
                    // if(parentProv == null) goto fail;
                    .Ldloc(parentRootProvider)
                    .Brfalse(fail)
                    // return parentProv.Root;
                    .Ldloc(parentRootProvider)
                    .EmitCall(mappings.RootObjectProvider.GetMethod(m => m.Name == "get_RootObject"))
                    .Ret()
                    // fail:
                    .MarkLabel(fail)
                    .Ldnull()
                    .Ret();

                if (mappings.RootObjectProviderIntermediateRootPropertyName != null)
                    ImplementInterfacePropertyGetter(builder, mappings.RootObjectProvider, mappings.RootObjectProviderIntermediateRootPropertyName)
                        .Generator
                        .LdThisFld(intermediateRootObjectField)
                        .Ret();

                ownServices.Add(mappings.RootObjectProvider);
            }

            if (mappings.ParentStackProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.ParentStackProvider);
                var objectListType = typeSystem.GetType("System.Collections.Generic.List`1")
                    .MakeGenericType(new[] {typeSystem.GetType("System.Object")});
                ParentListField = builder.DefineField(objectListType, XamlRuntimeContextDefintion.ParentListFieldName, XamlVisibility.Public, false);

                var enumerator = EmitParentEnumerable(typeSystem, parentBuilder, mappings);
                CreateCallbacks.Add(enumerator.createCallback);
                var parentStackEnumerableField = builder.DefineField(
                    typeSystem.GetType("System.Collections.Generic.IEnumerable`1").MakeGenericType(new[]{so}),
                    "_parentStackEnumerable", XamlVisibility.Private, false);

                ImplementInterfacePropertyGetter(builder, mappings.ParentStackProvider, "Parents")
                    .Generator.LdThisFld(parentStackEnumerableField).Ret();
                
                ctorCallbacks.Add(g => g
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, objectListType.GetConstructor())
                    .Emit(OpCodes.Stfld, ParentListField)
                    .Emit(OpCodes.Ldarg_0)
                    .LdThisFld(ParentListField)
                    .LdThisFld(ParentServiceProviderField)
                    .Emit(OpCodes.Newobj, enumerator.ctor)
                    .Emit(OpCodes.Stfld, parentStackEnumerableField));
                ownServices.Add(mappings.ParentStackProvider);
            }

            if (EmitTypeDescriptorContextStub(typeSystem, builder, mappings) is { } typeDescriptorContextType)
                ownServices.Add(typeDescriptorContextType);

            if (mappings.ProvideValueTarget != null)
            {
                builder.AddInterfaceImplementation(mappings.ProvideValueTarget);
                TargetObjectField = builder.DefineField(so, XamlRuntimeContextDefintion.ProvideTargetObjectName, XamlVisibility.Public, false);
                TargetPropertyField = builder.DefineField(so, XamlRuntimeContextDefintion.ProvideTargetPropertyName, XamlVisibility.Public, false);
                ImplementInterfacePropertyGetter(builder, mappings.ProvideValueTarget, "TargetObject")
                    .Generator.LdThisFld(TargetObjectField).Ret();
                ImplementInterfacePropertyGetter(builder, mappings.ProvideValueTarget, "TargetProperty")
                    .Generator.LdThisFld(TargetPropertyField).Ret();
                ownServices.Add(mappings.ProvideValueTarget);
            }

            IXamlField? baseUriField = null;
            if (mappings.UriContextProvider != null)
            {
                baseUriField = builder.DefineField(systemUri, "_baseUri", XamlVisibility.Private, false);
                builder.AddInterfaceImplementation(mappings.UriContextProvider);
                var getter = builder.DefineMethod(systemUri, Array.Empty<IXamlType>(), "get_BaseUri", XamlVisibility.Public, false, true);
                var setter = builder.DefineMethod(typeSystem.GetType("System.Void"), new[] {systemUri},
                    "set_BaseUri", XamlVisibility.Public, false, true);

                getter.Generator
                    .LdThisFld(baseUriField)
                    .Ret();

                setter.Generator
                    .Ldarg_0()
                    .Ldarg(1)
                    .Stfld(baseUriField)
                    .Ret();
                builder.DefineProperty(systemUri, "BaseUri", setter, getter);
                
                    
                ownServices.Add(mappings.UriContextProvider);    
            }
            
            builder.AddInterfaceImplementation(mappings.ServiceProvider);
            var getServiceMethod = builder.DefineMethod(so,
                new[] {systemType},
                "GetService", XamlVisibility.Public, false, true);

            ownServices = ownServices.Where(s => s != null).ToList();


            if (InnerServiceProviderField != null)
            {
                var next = getServiceMethod.Generator.DefineLabel();
                var innerResult = getServiceMethod.Generator.DefineLocal(so);
                getServiceMethod.Generator
                    //if(_inner == null) goto next;
                    .LdThisFld(InnerServiceProviderField)
                    .Brfalse(next)
                    // var innerRes = _inner.GetService(type);
                    .LdThisFld(InnerServiceProviderField)
                    .Ldarg(1)
                    .EmitCall(getServiceInterfaceMethod)
                    .Stloc(innerResult)
                    // if(innerRes == null) goto next;
                    .Ldloc(innerResult)
                    .Brfalse(next)
                    // return innerRes
                    .Ldloc(innerResult)
                    .Ret()
                    .MarkLabel(next);

            }
            var isAssignableFrom = systemType.GetMethod("IsAssignableFrom", typeSystem.GetType("System.Boolean"),
                false, systemType);
            var fromHandle = systemType.Methods.First(m => m.Name == "GetTypeFromHandle");
            var getTypeFromObject = so.Methods.First(m => m.Name == "GetType" && m.Parameters.Count == 0);
            if (ownServices.Count != 0)
            {
                var compare = systemType.GetMethod("Equals", typeSystem.GetType("System.Boolean"), false, systemType);

                for (var c = 0; c < ownServices.Count; c++)
                {
                    var next = getServiceMethod.Generator.DefineLabel();
                    getServiceMethod.Generator
                        .Emit(OpCodes.Ldtoken, ownServices[c])
                        .EmitCall(fromHandle)
                        .Emit(OpCodes.Ldarg_1)
                        .Emit(OpCodes.Callvirt, compare)
                        .Emit(OpCodes.Brfalse, next)
                        .Emit(OpCodes.Ldarg_0)
                        .Emit(OpCodes.Ret)
                        .MarkLabel(next);
                }
            }

            var staticProviderIndex = getServiceMethod.Generator.DefineLocal(typeSystem.GetType("System.Int32"));
            var staticProviderNext = getServiceMethod.Generator.DefineLabel();
            var staticProviderFailed = getServiceMethod.Generator.DefineLabel();
            var staticProviderEnd = getServiceMethod.Generator.DefineLabel();
            var staticProviderElement = getServiceMethod.Generator.DefineLocal(so);
            getServiceMethod.Generator
                //start: if(_staticProviders == null) goto: end
                .LdThisFld(staticProvidersField)
                .Brfalse(staticProviderEnd)
                // var c = 0
                .Ldc_I4(0)
                .Stloc(staticProviderIndex)
                // next:
                .MarkLabel(staticProviderNext)
                // if(c >= _staticProviders.Length) goto: end
                .Ldloc(staticProviderIndex)
                .LdThisFld(staticProvidersField)
                .Ldlen()
                .Bge(staticProviderEnd)
                // var obj = _staticProviders[c]
                .LdThisFld(staticProvidersField)
                .Ldloc(staticProviderIndex)
                .Ldelem_ref()
                // dup
                .Stloc(staticProviderElement)
                .Ldarg(1)
                .Ldloc(staticProviderElement)
                // if(obj.GetType().Equals(arg1)) return obj; else goto failed;
                .EmitCall(getTypeFromObject)
                .EmitCall(isAssignableFrom)
                .Brfalse(staticProviderFailed)
                .Ldloc(staticProviderElement)
                .Ret()
                // failed: 
                .MarkLabel(staticProviderFailed)
                // c++
                .Ldloc(staticProviderIndex)
                .Ldc_I4(1)
                .Add()
                .Stloc(staticProviderIndex)
                // goto: start
                .Br(staticProviderNext)
                // end:
                .MarkLabel(staticProviderEnd);                

            var noParentProvider = getServiceMethod.Generator.DefineLabel();
            getServiceMethod.Generator
                .LdThisFld(ParentServiceProviderField)
                .Brfalse(noParentProvider)
                .LdThisFld(ParentServiceProviderField)
                .Ldarg(1)
                .EmitCall(getServiceInterfaceMethod)
                .Emit(OpCodes.Ret)
                .MarkLabel(noParentProvider)
                .Ldnull()
                .Ret();

            var ctor = builder.DefineConstructor(false, 
                mappings.ServiceProvider,
                staticProvidersField.FieldType,
                systemString);
            ConstructorBuilder = ctor;

            ctor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors[0])
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, ParentServiceProviderField)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_2)
                .Emit(OpCodes.Stfld, staticProvidersField);
            if (baseUriField != null)
            {
                var noUri = ctor.Generator.DefineLabel();
                ctor.Generator
                    .Emit(OpCodes.Ldarg_3)
                    .Brfalse(noUri)
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Ldarg_3)
                    .Newobj(systemUri.GetConstructor(new List<IXamlType>
                    {
                        typeSystem.GetType("System.String")
                    }))
                    .Emit(OpCodes.Stfld, baseUriField)
                    .MarkLabel(noUri);
            }

            foreach (var feature in ctorCallbacks)
                feature(ctor.Generator);

            emitMappings.ContextTypeBuilderCallback?.Invoke(this);
            
            // We are calling this last to ensure that our own services are ready
            if (InnerServiceProviderField != null)
                ctor.Generator
                    // _innerSp = InnerServiceProviderFactory(this)
                    .Ldarg_0()
                    .Ldarg_0()
                    .EmitCall(mappings.InnerServiceProviderFactoryMethod!)
                    .Stfld(InnerServiceProviderField);
                    
            ctor.Generator.Emit(OpCodes.Ret);

            CreateCallbacks.Add(() => { parentBuilder.CreateType(); });
            
            if (ParentListField != null)
            {
                EmitPushPopParent(builder, typeSystem);
            }
            
            CreateAllTypes();
            ContextType = builder.CreateType();
        }

        public void CreateAllTypes()
        {
            foreach (var cb in CreateCallbacks)
                cb();
        }

        private void EmitPushPopParent(IXamlTypeBuilder<IXamlILEmitter> builder, IXamlTypeSystem ts)
        {
            Debug.Assert(ParentListField is not null);
            var parentListField = ParentListField!;

            var @void = ts.GetType("System.Void");
            var so = ts.GetType("System.Object");
            var  objectListType = ts.GetType("System.Collections.Generic.List`1")
                .MakeGenericType(new[] {so});
            
            var pushParentGenerator =
                builder.DefineMethod(@void, new[] {so}, XamlRuntimeContextDefintion.PushParentMethodName, XamlVisibility.Public, false, false)
                .Generator;

            pushParentGenerator.LdThisFld(parentListField)
                .Ldarg(1)
                .EmitCall(objectListType.GetMethod("Add", @void,
                    false, so));

            if (TargetObjectField != null)
            {
                pushParentGenerator.Ldarg_0()
                    .Ldarg(1)
                    .Stfld(TargetObjectField)
                    .Ret();
            }
            
            var pop = builder.DefineMethod(@void, Array.Empty<IXamlType>(), XamlRuntimeContextDefintion.PopParentMethodName, XamlVisibility.Public, false, false)
                .Generator;

            var idx = pop.DefineLocal(ts.GetType("System.Int32"));
            pop
                // var idx = _parents.Count - 1;
                .LdThisFld(parentListField)
                .EmitCall(objectListType.GetMethod(m => m.Name == "get_Count"))
                .Ldc_I4(1).Emit(OpCodes.Sub).Stloc(idx);

            pop
                // _parents.RemoveAt(idx);
                .LdThisFld(parentListField)
                .Ldloc(idx)
                .EmitCall(objectListType.GetMethod(m => m.Name == "RemoveAt"));

            // this.PropertyTargetObject = idx != 0 ? _parents[idx - 1] : null;
            if (TargetObjectField != null)
            {
                var elseLabel = pop.DefineLabel();
                var endIfLabel = pop.DefineLabel();
                pop
                    .Ldarg(0)
                    .Ldloc(idx)
                    .Brfalse(elseLabel) // if `idx != 0`
                    .LdThisFld(parentListField) // then '_parents[idx - 1]'
                    .Ldloc(idx)
                    .Ldc_I4(1).Emit(OpCodes.Sub)
                    .EmitCall(objectListType.GetMethod(m => m.Name == "get_Item"))
                    .Br(endIfLabel)
                    .MarkLabel(elseLabel) // else
                    .Ldnull() // then 'null'
                    .MarkLabel(endIfLabel) // endif
                    .Stfld(TargetObjectField);
            }

            pop.Ret();
        }
        
        private IXamlMethodBuilder<IXamlILEmitter> ImplementInterfacePropertyGetter(IXamlTypeBuilder<IXamlILEmitter> builder ,
            IXamlType type, string name)
        {
            var prefix = type.Namespace + "." + type.Name + ".";
            var originalGetter = type.GetMethod(m => m.Name == "get_" + name);
            var gen = builder.DefineMethod(originalGetter.ReturnType, [],
                prefix + "get_" + name, XamlVisibility.Private, false,
                true, originalGetter);
            builder.DefineProperty(originalGetter.ReturnType,prefix+ name, null, gen);
            return gen;
        }
        
        IXamlType? EmitTypeDescriptorContextStub(IXamlTypeSystem typeSystem, IXamlTypeBuilder<IXamlILEmitter> builder,
            XamlLanguageTypeMappings mappings)
        {
            if (mappings.TypeDescriptorContext == null)
                return null;
            var tdc = mappings.TypeDescriptorContext;
            var tdcPrefix = tdc.Namespace + "." + tdc.Name+".";

            builder.AddInterfaceImplementation(mappings.TypeDescriptorContext);
            void PropertyStub(string name) => ImplementInterfacePropertyGetter(builder, tdc, name).Generator.Ldnull().Ret();
            PropertyStub("Container");
            PropertyStub("Instance");
            PropertyStub("PropertyDescriptor");

            void MethodStub(string name)
            {
                var original = tdc.GetMethod(m => m.Name == name);
                builder.DefineMethod(original.ReturnType, original.Parameters, tdcPrefix + name, 
                        XamlVisibility.Private, false, true,
                        original)
                    .Generator
                    .Emit(OpCodes.Newobj,
                        typeSystem.GetType("System.NotSupportedException").GetConstructor())
                    .Emit(OpCodes.Throw);

            }
            MethodStub("OnComponentChanging");
            MethodStub("OnComponentChanged");

            return mappings.TypeDescriptorContext;
        }
        
        (IXamlType type, IXamlConstructor ctor, Action createCallback) EmitParentEnumerable(IXamlTypeSystem typeSystem, IXamlTypeBuilder<IXamlILEmitter> parentBuilder,
            XamlLanguageTypeMappings mappings)
        {
            Debug.Assert(ParentListField is not null);
            Debug.Assert(mappings.ParentStackProvider is not null);

            var parentListField = ParentListField!;
            var parentStackProvider = mappings.ParentStackProvider!;

            var so = typeSystem.GetType("System.Object");
            var enumerableBuilder =
                parentBuilder.DefineSubType(typeSystem.GetType("System.Object"), "ParentStackEnumerable", XamlVisibility.Private);
            var enumerableType = typeSystem.GetType("System.Collections.Generic.IEnumerable`1")
                .MakeGenericType(new[] {typeSystem.GetType("System.Object")});
            enumerableBuilder.AddInterfaceImplementation(enumerableType);

            var enumerableParentList =
                enumerableBuilder.DefineField(parentListField.FieldType, "_parentList", XamlVisibility.Private, false);
            var enumerableParentProvider =
                enumerableBuilder.DefineField(mappings.ServiceProvider, "_parentSP", XamlVisibility.Private, false);
            
            var enumerableCtor = enumerableBuilder.DefineConstructor(false, enumerableParentList.FieldType, enumerableParentProvider.FieldType);
            enumerableCtor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors[0])
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, enumerableParentList)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_2)
                .Emit(OpCodes.Stfld, enumerableParentProvider)
                .Emit(OpCodes.Ret);


            var enumeratorType = typeSystem.GetType("System.Collections.IEnumerator");
            var enumeratorObjectType = typeSystem.GetType("System.Collections.Generic.IEnumerator`1")
                .MakeGenericType(new[] {so});

            var enumeratorBuilder = enumerableBuilder.DefineSubType(so, "Enumerator", XamlVisibility.Public);
            enumeratorBuilder.AddInterfaceImplementation(enumeratorObjectType);


            
            var state = enumeratorBuilder.DefineField(typeSystem.GetType("System.Int32"), "_state", XamlVisibility.Private, false);
            
            
            var parentList =
                enumeratorBuilder.DefineField(parentListField.FieldType, "_parentList", XamlVisibility.Private, false);
            var parentProvider =
                enumeratorBuilder.DefineField(mappings.ServiceProvider, "_parentSP", XamlVisibility.Private, false);
            var listType = typeSystem.GetType("System.Collections.Generic.List`1")
                .MakeGenericType(new[] {so});
            var list = enumeratorBuilder.DefineField(listType, "_list", XamlVisibility.Private, false);
            var listIndex = enumeratorBuilder.DefineField(typeSystem.GetType("System.Int32"), "_listIndex", XamlVisibility.Private, false);
            var current = enumeratorBuilder.DefineField(so, "_current", XamlVisibility.Private, false);
            var parentEnumerator = enumeratorBuilder.DefineField(enumeratorObjectType, "_parentEnumerator", XamlVisibility.Private, false);
            var enumeratorCtor = enumeratorBuilder.DefineConstructor(false, parentList.FieldType, parentProvider.FieldType);
                enumeratorCtor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors[0])
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, parentList)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_2)
                .Emit(OpCodes.Stfld, parentProvider)
                .Emit(OpCodes.Ret);
            
            var currentGetter = enumeratorBuilder.DefineMethod(so, [],
                "get_Current", XamlVisibility.Public, false, true);
            enumeratorBuilder.DefineProperty(so, "Current", null, currentGetter);
            currentGetter.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, current)
                .Emit(OpCodes.Ret);

            enumeratorBuilder.DefineMethod(typeSystem.GetType("System.Void"), Array.Empty<IXamlType>(),
                    enumeratorObjectType.FullName + ".Reset", XamlVisibility.Private, false,
                    true, enumeratorObjectType.FindMethod(m => m.Name == "Reset")).Generator
                .Emit(OpCodes.Newobj,
                    typeSystem.GetType("System.NotSupportedException").GetConstructor())
                .Emit(OpCodes.Throw);

            var disposeGen = enumeratorBuilder.DefineMethod(typeSystem.GetType("System.Void"), Array.Empty<IXamlType>(),
                "System.IDisposable.Dispose", XamlVisibility.Private, false, true,
                typeSystem.GetType("System.IDisposable").FindMethod(m => m.Name == "Dispose")).Generator;
            var disposeRet = disposeGen.DefineLabel();
            disposeGen
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, parentEnumerator)
                .Emit(OpCodes.Brfalse, disposeRet)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, parentEnumerator)
                .Emit(OpCodes.Callvirt, typeSystem.GetType("System.IDisposable").GetMethod(m => m.Name == "Dispose"))
                .MarkLabel(disposeRet)
                .Emit(OpCodes.Ret);

            var boolType = typeSystem.GetType("System.Boolean");
            var moveNext = enumeratorBuilder.DefineMethod(boolType, Array.Empty<IXamlType>(),
                enumeratorObjectType.FullName+".MoveNext", XamlVisibility.Private,
                false, true,
                enumeratorObjectType.GetMethod(m => m.Name == "MoveNext")).Generator;

            
            const int stateInit = 0;
            const int stateSelf = 1;
            const int stateParent = 2;
            const int stateEof = 3;
            
            var checkStateInit = moveNext.DefineLabel();
            var checkStateSelf = moveNext.DefineLabel();
            var checkStateParent = moveNext.DefineLabel();
            var eof = moveNext.DefineLabel();

            moveNext
                .LdThisFld(state).Ldc_I4(stateInit).Beq(checkStateInit)
                .LdThisFld(state).Ldc_I4(stateSelf).Beq(checkStateSelf)
                .LdThisFld(state).Ldc_I4(stateParent).Beq(checkStateParent)
                .Ldc_I4(0).Ret();
            moveNext.MarkLabel(checkStateInit)
                .Ldarg_0().Dup().Dup().Ldfld(parentList).Stfld(list).LdThisFld(list)
                .EmitCall(listType.GetMethod(m => m.Name == "get_Count" && m.Parameters.Count == 0))
                .Ldc_I4(1).Emit(OpCodes.Sub).Stfld(listIndex)
                .Ldarg_0().Ldc_I4(stateSelf).Stfld(state)
                .Br(checkStateSelf);
            var tryParentState = moveNext.DefineLabel();
            var parentProv = moveNext.DefineLocal(parentStackProvider);
            moveNext.MarkLabel(checkStateSelf)

                // if(_listIndex<0) goto tryParent
                .LdThisFld(listIndex).Ldc_I4(0).Emit(OpCodes.Blt, tryParentState)
                // _current = _list[_listIndex]
                .Ldarg_0().LdThisFld(list).LdThisFld(listIndex)
                .EmitCall(listType.GetMethod(m => m.Name == "get_Item")).Stfld(current)
                // _listIndex--
                .Ldarg_0().LdThisFld(listIndex).Ldc_I4(1).Emit(OpCodes.Sub).Stfld(listIndex)
                // return true
                .Ldc_I4(1).Ret()
                // tryParent:
                .MarkLabel(tryParentState)
                // if(parent._parentServiceProvider == null) goto eof;
                .LdThisFld(parentProvider).Brfalse(eof)
                // parentProv = (IParentStackProvider)parent.GetService(typeof(IParentStackProvider));
                .LdThisFld(parentProvider).Ldtype(parentStackProvider)
                .EmitCall(mappings.ServiceProvider.GetMethod("GetService", so, false,
                    typeSystem.GetType("System.Type")))
                .Emit(OpCodes.Castclass, parentStackProvider)
                .Dup().Stloc(parentProv)
                // if(parentProv == null) goto eof
                .Brfalse(eof)
                // _parentEnumerator = parentProv.Parents.GetEnumerator()
                .Ldarg_0().Ldloc(parentProv)
                .EmitCall(parentStackProvider.GetMethod(m => m.Name == "get_Parents"))
                .EmitCall(enumerableType.GetMethod(m => m.Name == "GetEnumerator"))
                .Stfld(parentEnumerator)
                .Ldarg_0().Ldc_I4(stateParent).Stfld(state);


            moveNext.MarkLabel(checkStateParent)
                // if(!_parentEnumerator.MoveNext()) goto eof
                .LdThisFld(parentEnumerator).EmitCall(enumeratorType.GetMethod("MoveNext", boolType, false))
                .Brfalse(eof)
                // _current = _parentEnumerator.Current
                .Ldarg_0()
                .LdThisFld(parentEnumerator).EmitCall(enumeratorObjectType.GetMethod("get_Current", so, false))
                .Stfld(current)
                // return true
                .Ldc_I4(1).Ret();
            
            moveNext.MarkLabel(eof)
                .Ldarg_0().Ldc_I4(stateEof).Stfld(state)
                .Ldc_I4(0).Ret();
                
            var createEnumerator = enumerableBuilder.DefineMethod(enumeratorObjectType, Array.Empty<IXamlType>(), "GetEnumerator", XamlVisibility.Public, false,
                    true);
            createEnumerator.Generator
                .LdThisFld(enumerableParentList)
                .LdThisFld(enumerableParentProvider)
                .Emit(OpCodes.Newobj, enumeratorCtor)
                .Emit(OpCodes.Ret);

            enumerableBuilder.DefineMethod(enumeratorType, Array.Empty<IXamlType>(),
                    "System.Collections.IEnumerable.GetEnumerator", XamlVisibility.Private, false, true,
                    typeSystem.GetType("System.Collections.IEnumerable").FindMethod(m => m.Name == "GetEnumerator"))
                .Generator
                .Ldarg_0()
                .EmitCall(createEnumerator)
                .Emit(OpCodes.Ret);



            return (enumeratorBuilder, enumerableCtor, () =>
            {
                enumeratorBuilder.CreateType();
                enumerableBuilder.CreateType();
            });
        }
    }
}
