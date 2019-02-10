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
        public IXamlField ParentListField { get; set; }
        public IXamlType ContextType { get; set; }
        private IXamlField _parentStackEnumerableField;
        private IXamlField _parentServiceProviderField;
        private IXamlField _innerServiceProviderField;
        public IXamlField PropertyTargetObject { get; set; }
        public IXamlField PropertyTargetProperty { get; set; }

        /// <summary>
        /// Ctor expects IServiceProvider
        /// </summary>
        public IXamlConstructor Constructor { get; set; }

        public static XamlContext GenerateContextClass(IXamlTypeBuilder builder,
            IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings,
            IXamlType rootType, IEnumerable<IXamlField> staticProviders, string baseUri) 
            => new XamlContext(builder, typeSystem, mappings, rootType, staticProviders, baseUri);

        public List<Action> CreateCallbacks = new List<Action>();
        
        private XamlContext(IXamlTypeBuilder builder,
            IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings,
            IXamlType rootType, IEnumerable<IXamlField> staticProviders, string baseUri)
        {
            RootObjectField = builder.DefineField(rootType, "RootObject", true, false);
            _parentServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_sp", false, false);
            if (mappings.InnerServiceProviderFactoryMethod != null)
                _innerServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_innerSp", false, false);
            var so = typeSystem.GetType("System.Object");
            var systemType = typeSystem.GetType("System.Type");
            var getServiceInterfaceMethod = mappings.ServiceProvider.FindMethod("GetService", so, false, systemType);

            var ownServices = new List<IXamlType>();
            var ctorCallbacks = new List<Action<IXamlILEmitter>>();
            
            if (mappings.RootObjectProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.RootObjectProvider);
                var rootGen = ImplementInterfacePropertyGetter(builder, mappings.RootObjectProvider, "RootObject")
                    .Generator;
                var tryParent = rootGen.DefineLabel();
                var fail = rootGen.DefineLabel();
                var parentRootProvider = rootGen.DefineLocal(mappings.RootObjectProvider);
                rootGen
                    // if(RootObject!=null) return RootObject;    
                    .LdThisFld(RootObjectField)
                    .Brfalse(tryParent)
                    .LdThisFld(RootObjectField)
                    .Ret()
                    // if(_sp == null) goto fail;
                    .MarkLabel(tryParent)
                    .LdThisFld(_parentServiceProviderField)
                    .Brfalse(fail)
                    // parentProv =  (IRootObjectProvider)_sp.GetService(typeof(IRootObjectProvider));
                    .LdThisFld(_parentServiceProviderField)
                    .Ldtype(mappings.RootObjectProvider)
                    .EmitCall(getServiceInterfaceMethod)
                    .Castclass(mappings.RootObjectProvider)
                    .Stloc(parentRootProvider)
                    // if(parentProv == null) goto fail;
                    .Ldloc(parentRootProvider)
                    .Brfalse(fail)
                    // return parentProv.Root;
                    .Ldloc(parentRootProvider)
                    .EmitCall(mappings.RootObjectProvider.FindMethod(m => m.Name == "get_RootObject"))
                    .Ret()
                    // fail:
                    .MarkLabel(fail)
                    .Ldnull()
                    .Ret();
                
                ownServices.Add(mappings.RootObjectProvider);
            }

            if (mappings.ParentStackProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.ParentStackProvider);
                var objectListType = typeSystem.GetType("System.Collections.Generic.List`1")
                    .MakeGenericType(new[] {typeSystem.GetType("System.Object")});
                ParentListField = builder.DefineField(objectListType, "ParentsStack", true, false);

                var enumerator = EmitParentEnumerable(typeSystem, builder, mappings);
                CreateCallbacks.Add(enumerator.createCallback);
                _parentStackEnumerableField = builder.DefineField(
                    typeSystem.GetType("System.Collections.Generic.IEnumerable`1").MakeGenericType(new[]{so}),
                    "_parentStackEnumerable", false, false);

                ImplementInterfacePropertyGetter(builder, mappings.ParentStackProvider, "Parents")
                    .Generator.LdThisFld(_parentStackEnumerableField).Ret();
                
                ctorCallbacks.Add(g => g
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, objectListType.FindConstructor(new List<IXamlType>()))
                    .Emit(OpCodes.Stfld, ParentListField)
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, enumerator.ctor)
                    .Emit(OpCodes.Stfld, _parentStackEnumerableField));
                ownServices.Add(mappings.ParentStackProvider);
            }

            ownServices.Add(EmitTypeDescriptorContextStub(typeSystem, builder, mappings));

            if (mappings.ProvideValueTarget != null)
            {
                builder.AddInterfaceImplementation(mappings.ProvideValueTarget);
                PropertyTargetObject = builder.DefineField(so, "ProvideTargetObject", true, false);
                PropertyTargetProperty = builder.DefineField(so, "ProvideTargetProperty", true, false);
                ImplementInterfacePropertyGetter(builder, mappings.ProvideValueTarget, "TargetObject")
                    .Generator.LdThisFld(PropertyTargetObject).Ret();
                ImplementInterfacePropertyGetter(builder, mappings.ProvideValueTarget, "TargetProperty")
                    .Generator.LdThisFld(PropertyTargetProperty).Ret();
                ownServices.Add(mappings.ProvideValueTarget);
            }

            if (mappings.UriContextProvider != null)
            {
                var systemUri = typeSystem.GetType("System.Uri");
                var cached = builder.DefineField(systemUri, "_baseUri", false, false);
                builder.AddInterfaceImplementation(mappings.UriContextProvider);
                var getter = builder.DefineMethod(systemUri, new IXamlType[0], "get_BaseUri", true, false, true);
                var setter = builder.DefineMethod(typeSystem.GetType("System.Void"), new[] {systemUri},
                    "set_BaseUri", true, false, true);


                var noCache = getter.Generator.DefineLabel();
                getter.Generator
                    .LdThisFld(cached)
                    .Brfalse(noCache)
                    .LdThisFld(cached)
                    .Ret()
                    .MarkLabel(noCache)
                    .Ldarg_0()
                    .Ldstr(baseUri)
                    .Newobj(systemUri.FindConstructor(new List<IXamlType>
                    {
                        typeSystem.GetType("System.String")
                    }))
                    .Stfld(cached)
                    .LdThisFld(cached)
                    .Ret();

                setter.Generator
                    .Ldarg_0()
                    .Ldarg(1)
                    .Stfld(cached)
                    .Ret();
                builder.DefineProperty(systemUri, "BaseUri", setter, getter);
                
                    
                ownServices.Add(mappings.UriContextProvider);    
            }
            
            builder.AddInterfaceImplementation(mappings.ServiceProvider);
            var getServiceMethod = builder.DefineMethod(so,
                new[] {systemType},
                "GetService", true, false, true);

            ownServices = ownServices.Where(s => s != null).ToList();


            if (_innerServiceProviderField != null)
            {
                var next = getServiceMethod.Generator.DefineLabel();
                var innerResult = getServiceMethod.Generator.DefineLocal(so);
                getServiceMethod.Generator
                    //if(_inner == null) goto next;
                    .LdThisFld(_innerServiceProviderField)
                    .Brfalse(next)
                    // var innerRes = _inner.GetService(type);
                    .LdThisFld(_innerServiceProviderField)
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
            var compare = systemType.FindMethod("Equals", typeSystem.GetType("System.Boolean"),
                false, systemType);
            var fromHandle = systemType.Methods.First(m => m.Name == "GetTypeFromHandle");
            if (ownServices.Count != 0)
            {

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
            
            if(staticProviders!=null)
                foreach (var sprov in staticProviders)
                {
                    var next = getServiceMethod.Generator.DefineLabel();
                    getServiceMethod.Generator
                        .Ldtoken(sprov.FieldType)
                        .EmitCall(fromHandle)
                        .Ldarg(1)
                        .EmitCall(compare)
                        .Brfalse(next)
                        .Ldsfld(sprov)
                        .Ret()
                        .MarkLabel(next);
                }

            getServiceMethod.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, _parentServiceProviderField)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Callvirt, getServiceInterfaceMethod)
                .Emit(OpCodes.Ret);

            var ctor = builder.DefineConstructor(false, mappings.ServiceProvider);
            ctor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, _parentServiceProviderField);
            foreach (var feature in ctorCallbacks)
                feature(ctor.Generator);
            
            // We are calling this last to ensure that our own services are ready
            if (_innerServiceProviderField != null)
                ctor.Generator
                    // _innerSp = InnerServiceProviderFactory(this)
                    .Ldarg_0()
                    .Ldarg_0()
                    .EmitCall(mappings.InnerServiceProviderFactoryMethod)
                    .Stfld(_innerServiceProviderField);
                    
            ctor.Generator.Emit(OpCodes.Ret);

            Constructor = ctor;
            CreateCallbacks.Add(() => { ContextType = builder.CreateType(); });
            ContextType = builder;
        }

        public void CreateAllTypes()
        {
            foreach (var cb in CreateCallbacks)
                cb();
        }
        
        private IXamlMethodBuilder ImplementInterfacePropertyGetter(IXamlTypeBuilder builder ,
            IXamlType type, string name)
        {
            var prefix = type.Namespace + "." + type.Name + ".";
            var originalGetter = type.FindMethod(m => m.Name == "get_" + name);
            var gen = builder.DefineMethod(originalGetter.ReturnType, new IXamlType[0],
                prefix + "get_" + name, false, false,
                true, originalGetter);
            builder.DefineProperty(originalGetter.ReturnType,prefix+ name, null, gen);
            return gen;
        }
        
        IXamlType EmitTypeDescriptorContextStub(IXamlTypeSystem typeSystem, IXamlTypeBuilder builder,
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
                var original = tdc.FindMethod(m => m.Name == name);
                builder.DefineMethod(original.ReturnType, original.Parameters, tdcPrefix + name, 
                        false, false, true,
                        original)
                    .Generator
                    .Emit(OpCodes.Newobj,
                        typeSystem.FindType("System.NotSupportedException").FindConstructor(new List<IXamlType>()))
                    .Emit(OpCodes.Throw);

            }
            MethodStub("OnComponentChanging");
            MethodStub("OnComponentChanged");

            return mappings.TypeDescriptorContext;
        }
        
        (IXamlType type, IXamlConstructor ctor, Action createCallback) EmitParentEnumerable(IXamlTypeSystem typeSystem, IXamlTypeBuilder parentBuilder,
            XamlLanguageTypeMappings mappings)
        {
            var so = typeSystem.GetType("System.Object");
            var enumerableBuilder =
                parentBuilder.DefineSubType(typeSystem.GetType("System.Object"), "ParentStackEnumerable", false);
            var enumerableType = typeSystem.GetType("System.Collections.Generic.IEnumerable`1")
                .MakeGenericType(new[] {typeSystem.GetType("System.Object")});
            enumerableBuilder.AddInterfaceImplementation(enumerableType);

            var enumerableParent = enumerableBuilder.DefineField(parentBuilder, "_parent", false, false);
            var enumerableCtor = enumerableBuilder.DefineConstructor(false, parentBuilder);
            enumerableCtor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, enumerableParent)
                .Emit(OpCodes.Ret);


            var enumeratorType = typeSystem.GetType("System.Collections.IEnumerator");
            var enumeratorObjectType = typeSystem.GetType("System.Collections.Generic.IEnumerator`1")
                .MakeGenericType(new[] {so});

            var enumeratorBuilder = enumerableBuilder.DefineSubType(so, "Enumerator", true);
            enumeratorBuilder.AddInterfaceImplementation(enumeratorObjectType);


            
            var state = enumeratorBuilder.DefineField(typeSystem.GetType("System.Int32"), "_state", false, false);
            var parent = enumeratorBuilder.DefineField(parentBuilder, "_parent", false, false);
            var listType = typeSystem.GetType("System.Collections.Generic.List`1")
                .MakeGenericType(new[] {so});
            var list = enumeratorBuilder.DefineField(listType, "_list", false, false);
            var listIndex = enumeratorBuilder.DefineField(typeSystem.GetType("System.Int32"), "_listIndex", false, false);
            var current = enumeratorBuilder.DefineField(so, "_current", false, false);
            var parentEnumerator = enumeratorBuilder.DefineField(enumeratorObjectType, "_parentEnumerator", false, false);
            var enumeratorCtor = enumeratorBuilder.DefineConstructor(false, parentBuilder);
                enumeratorCtor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, parent)
                .Emit(OpCodes.Ret);
            
            var currentGetter = enumeratorBuilder.DefineMethod(so, new IXamlType[0],
                "get_Current", true, false, true);
            enumeratorBuilder.DefineProperty(so, "Current", null, currentGetter);
            currentGetter.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, current)
                .Emit(OpCodes.Ret);

            enumeratorBuilder.DefineMethod(typeSystem.FindType("System.Void"), new IXamlType[0], "Reset", true, false,
                    true).Generator
                .Emit(OpCodes.Newobj,
                    typeSystem.FindType("System.NotSupportedException").FindConstructor(new List<IXamlType>()))
                .Emit(OpCodes.Throw);
            
            var disposeGen = enumeratorBuilder.DefineMethod(typeSystem.FindType("System.Void"), new IXamlType[0], 
                "Dispose", true, false, true ).Generator;
            var disposeRet = disposeGen.DefineLabel();
            disposeGen
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, parentEnumerator)
                .Emit(OpCodes.Brfalse, disposeRet)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, parentEnumerator)
                .Emit(OpCodes.Callvirt, typeSystem.GetType("System.IDisposable").FindMethod(m => m.Name == "Dispose"))
                .MarkLabel(disposeRet)
                .Emit(OpCodes.Ret);

            var boolType = typeSystem.GetType("System.Boolean");
            var moveNext = enumeratorBuilder.DefineMethod(boolType, new IXamlType[0],
                "MoveNext", true,
                false, true).Generator;

            
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
                .Ldarg_0().Dup().Dup().Ldfld(parent).Ldfld(ParentListField).Stfld(list).LdThisFld(list)
                .EmitCall(listType.FindMethod(m => m.Name == "get_Count" && m.Parameters.Count == 0))
                .Ldc_I4(1).Emit(OpCodes.Sub).Stfld(listIndex)
                .Ldarg_0().Ldc_I4(stateSelf).Stfld(state)
                .Br(checkStateSelf);
            var tryParentState = moveNext.DefineLabel();
            var parentProv = moveNext.DefineLocal(mappings.ParentStackProvider);
            moveNext.MarkLabel(checkStateSelf)

                // if(_listIndex<0) goto tryParent
                .LdThisFld(listIndex).Ldc_I4(0).Emit(OpCodes.Blt, tryParentState)
                // _current = _list[_listIndex]
                .Ldarg_0().LdThisFld(list).LdThisFld(listIndex)
                .EmitCall(listType.FindMethod(m => m.Name == "get_Item")).Stfld(current)
                // _listIndex--
                .Ldarg_0().LdThisFld(listIndex).Ldc_I4(1).Emit(OpCodes.Sub).Stfld(listIndex)
                // return true
                .Ldc_I4(1).Ret()
                // tryParent:
                .MarkLabel(tryParentState)
                // parentProv = (IParentStackProvider)parent.GetService(typeof(IParentStackProvider));
                .LdThisFld(parent).Ldfld(_parentServiceProviderField).Ldtype(mappings.ParentStackProvider)
                .EmitCall(mappings.ServiceProvider.FindMethod("GetService", so, false,
                    typeSystem.GetType("System.Type")))
                .Emit(OpCodes.Castclass, mappings.ParentStackProvider)
                .Dup().Stloc(parentProv)
                // if(parentProv == null) goto eof
                .Brfalse(eof)
                // _parentEnumerator = parentProv.Parents.GetEnumerator()
                .Ldarg_0().Ldloc(parentProv)
                .EmitCall(mappings.ParentStackProvider.FindMethod(m => m.Name == "get_Parents"))
                .EmitCall(enumerableType.FindMethod(m => m.Name == "GetEnumerator"))
                .Stfld(parentEnumerator)
                .Ldarg_0().Ldc_I4(stateParent).Stfld(state);


            moveNext.MarkLabel(checkStateParent)
                // if(!_parentEnumerator.MoveNext()) goto eof
                .LdThisFld(parentEnumerator).EmitCall(enumeratorType.FindMethod("MoveNext", boolType, false))
                .Brfalse(eof)
                // _current = _parentEnumerator.Current
                .Ldarg_0()
                .LdThisFld(parentEnumerator).EmitCall(enumeratorObjectType.FindMethod("get_Current", so, false))
                .Stfld(current)
                // return true
                .Ldc_I4(1).Ret();
            
            moveNext.MarkLabel(eof)
                .Ldarg_0().Ldc_I4(stateEof).Stfld(state)
                .Ldc_I4(0).Ret();
                
            var createEnumerator = enumerableBuilder.DefineMethod(enumeratorObjectType, new IXamlType[0], "GetEnumerator", true, false,
                    true);
            createEnumerator.Generator
                .Ldarg_0()
                .Ldfld(enumerableParent)
                .Emit(OpCodes.Newobj, enumeratorCtor)
                .Emit(OpCodes.Ret);

            enumerableBuilder.DefineMethod(enumeratorType, new IXamlType[0],
                    "System.Collections.IEnumerable.GetEnumerator", false, false, true,
                    typeSystem.GetType("System.Collections.IEnumerable").FindMethod(m => m.Name == "GetEnumerator"))
                .Generator
                .Ldarg_0()
                .Emit(OpCodes.Call, createEnumerator)
                .Emit(OpCodes.Ret);



            return (enumeratorBuilder, enumerableCtor, () =>
            {
                enumeratorBuilder.CreateType();
                enumerableBuilder.CreateType();
            });
        }
    }
}