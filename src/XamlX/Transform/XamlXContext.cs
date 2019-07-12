using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXContext
    {
        public IXamlXField RootObjectField { get; set; }
        public IXamlXField IntermediateRootObjectField { get; set; }
        public IXamlXField ParentListField { get; set; }
        public IXamlXType ContextType { get; set; }
        public IXamlXField PropertyTargetObject { get; set; }
        public IXamlXField PropertyTargetProperty { get; set; }
        public IXamlXConstructor Constructor { get; set; }
        public Action<IXamlXEmitter> Factory { get; set; }
        public IXamlXMethod PushParentMethod { get; set; }
        public IXamlXMethod PopParentMethod { get; set; }
        
        public XamlXContext(IXamlXType definition, IXamlXType constructedType,
            XamlXLanguageTypeMappings mappings,
            Action<XamlXContext, IXamlXEmitter> factory)
        {
            ContextType = definition.MakeGenericType(constructedType);

            IXamlXField Get(string s) =>
                ContextType.Fields.FirstOrDefault(f => f.Name == s);
            
            IXamlXMethod GetMethod(string s) =>
                ContextType.Methods.FirstOrDefault(f => f.Name == s);

            RootObjectField = Get(XamlXContextDefinition.RootObjectFieldName);
            IntermediateRootObjectField = Get(XamlXContextDefinition.IntermediateRootObjectFieldName);
            ParentListField = Get(XamlXContextDefinition.ParentListFieldName);
            PropertyTargetObject = Get(XamlXContextDefinition.ProvideTargetObjectName);
            PropertyTargetProperty = Get(XamlXContextDefinition.ProvideTargetPropertyName);
            PushParentMethod = GetMethod(XamlXContextDefinition.PushParentMethodName);
            PopParentMethod = GetMethod(XamlXContextDefinition.PopParentMethodName);
            Constructor = ContextType.Constructors.First();
            Factory = il => factory(this, il);
            if (mappings.ContextFactoryCallback != null)
                Factory = il =>
                {
                    factory(this, il);
                    mappings.ContextFactoryCallback(this, il);
                };
        }        

        public XamlXContext(IXamlXType definition, IXamlXType constructedType,
            XamlXLanguageTypeMappings mappings,
            string baseUri, List<IXamlXField> staticProviders) : this(definition, constructedType, mappings,
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
    
    
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXContextDefinition
    {
        public const string RootObjectFieldName = "RootObject";
        public const string IntermediateRootObjectFieldName = "IntermediateRoot";
        public const string ParentListFieldName = "ParentsStack";
        public const string ProvideTargetObjectName = "ProvideTargetObject";
        public const string ProvideTargetPropertyName = "ProvideTargetProperty";
        public const string PushParentMethodName = "PushParent";
        public const string PopParentMethodName = "PopParent";

        private IXamlXField ParentListField;
        private readonly IXamlXField _parentServiceProviderField;
        private readonly IXamlXField _innerServiceProviderField;
        private IXamlXField PropertyTargetObject;
        private IXamlXField PropertyTargetProperty;

        private IXamlXConstructor Constructor { get; set; }

        public static IXamlXType GenerateContextClass(IXamlXTypeBuilder builder,
            IXamlXTypeSystem typeSystem, XamlXLanguageTypeMappings mappings)
        {
            return new XamlXContextDefinition(builder, typeSystem, mappings).ContextType;

        }

        public List<Action> CreateCallbacks = new List<Action>();

        public const int BaseUriArg = 0;
        public const int StaticProvidersArg = 1;
        public IXamlXType ContextType;
        
        private XamlXContextDefinition(IXamlXTypeBuilder parentBuilder,
            IXamlXTypeSystem typeSystem, XamlXLanguageTypeMappings mappings)
        {
            var so = typeSystem.GetType("System.Object");
            var builder = parentBuilder.DefineSubType(so, "Context", true);
            
            builder.DefineGenericParameters(new[]
            {
                new KeyValuePair<string, XamlXGenericParameterConstraint>("TTarget",
                    new XamlXGenericParameterConstraint
                    {
                        IsClass = true
                    })
            });
            var rootObjectField = builder.DefineField(builder.GenericParameters[0], "RootObject", true, false);
            var intermediateRootObjectField = builder.DefineField(so, IntermediateRootObjectFieldName, true, false);
            _parentServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_sp", false, false);
            if (mappings.InnerServiceProviderFactoryMethod != null)
                _innerServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_innerSp", false, false);

            var staticProvidersField = builder.DefineField(typeSystem.GetType("System.Object").MakeArrayType(1),
                "_staticProviders", false, false);
            
            
            var systemType = typeSystem.GetType("System.Type");
            var systemUri = typeSystem.GetType("System.Uri");
            var systemString = typeSystem.GetType("System.String");
            var getServiceInterfaceMethod = mappings.ServiceProvider.FindMethod("GetService", so, false, systemType);

            var ownServices = new List<IXamlXType>();
            var ctorCallbacks = new List<Action<IXamlXEmitter>>();
            
            if (mappings.RootObjectProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.RootObjectProvider);
                var rootGen = ImplementInterfacePropertyGetter(builder, mappings.RootObjectProvider, RootObjectFieldName)
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
                ParentListField = builder.DefineField(objectListType, ParentListFieldName, true, false);

                var enumerator = EmitParentEnumerable(typeSystem, parentBuilder, mappings);
                CreateCallbacks.Add(enumerator.createCallback);
                var parentStackEnumerableField = builder.DefineField(
                    typeSystem.GetType("System.Collections.Generic.IEnumerable`1").MakeGenericType(new[]{so}),
                    "_parentStackEnumerable", false, false);

                ImplementInterfacePropertyGetter(builder, mappings.ParentStackProvider, "Parents")
                    .Generator.LdThisFld(parentStackEnumerableField).Ret();
                
                ctorCallbacks.Add(g => g
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, objectListType.FindConstructor(new List<IXamlXType>()))
                    .Emit(OpCodes.Stfld, ParentListField)
                    .Emit(OpCodes.Ldarg_0)
                    .LdThisFld(ParentListField)
                    .LdThisFld(_parentServiceProviderField)
                    .Emit(OpCodes.Newobj, enumerator.ctor)
                    .Emit(OpCodes.Stfld, parentStackEnumerableField));
                ownServices.Add(mappings.ParentStackProvider);
            }

            ownServices.Add(EmitTypeDescriptorContextStub(typeSystem, builder, mappings));

            if (mappings.ProvideValueTarget != null)
            {
                builder.AddInterfaceImplementation(mappings.ProvideValueTarget);
                PropertyTargetObject = builder.DefineField(so, ProvideTargetObjectName, true, false);
                PropertyTargetProperty = builder.DefineField(so, ProvideTargetPropertyName, true, false);
                ImplementInterfacePropertyGetter(builder, mappings.ProvideValueTarget, "TargetObject")
                    .Generator.LdThisFld(PropertyTargetObject).Ret();
                ImplementInterfacePropertyGetter(builder, mappings.ProvideValueTarget, "TargetProperty")
                    .Generator.LdThisFld(PropertyTargetProperty).Ret();
                ownServices.Add(mappings.ProvideValueTarget);
            }

            IXamlXField baseUriField = null;
            if (mappings.UriContextProvider != null)
            {
                baseUriField = builder.DefineField(systemUri, "_baseUri", false, false);
                builder.AddInterfaceImplementation(mappings.UriContextProvider);
                var getter = builder.DefineMethod(systemUri, new IXamlXType[0], "get_BaseUri", true, false, true);
                var setter = builder.DefineMethod(typeSystem.GetType("System.Void"), new[] {systemUri},
                    "set_BaseUri", true, false, true);

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
            var isAssignableFrom = systemType.FindMethod("IsAssignableFrom", typeSystem.GetType("System.Boolean"),
                false, systemType);
            var fromHandle = systemType.Methods.First(m => m.Name == "GetTypeFromHandle");
            var getTypeFromObject = so.Methods.First(m => m.Name == "GetType" && m.Parameters.Count == 0);
            if (ownServices.Count != 0)
            {

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
                .LdThisFld(_parentServiceProviderField)
                .Brfalse(noParentProvider)
                .LdThisFld(_parentServiceProviderField)
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
            ctor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, _parentServiceProviderField)
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
                    .Newobj(systemUri.FindConstructor(new List<IXamlXType>
                    {
                        typeSystem.GetType("System.String")
                    }))
                    .Emit(OpCodes.Stfld, baseUriField)
                    .MarkLabel(noUri);
            }

            foreach (var feature in ctorCallbacks)
                feature(ctor.Generator);
            
            mappings.ContextTypeBuilderCallback?.Invoke(builder, ctor.Generator);
            
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
            CreateCallbacks.Add(() => { parentBuilder.CreateType(); });
            
            EmitPushPopParent(builder, typeSystem);
            
            CreateAllTypes();
            ContextType = builder.CreateType();
        }

        public void CreateAllTypes()
        {
            foreach (var cb in CreateCallbacks)
                cb();
        }

        private void EmitPushPopParent(IXamlXTypeBuilder builder, IXamlXTypeSystem ts)
        {
            var @void = ts.GetType("System.Void");
            var so = ts.GetType("System.Object");
            var  objectListType = ts.GetType("System.Collections.Generic.List`1")
                .MakeGenericType(new[] {so});
            
            builder.DefineMethod(@void, new[] {so}, PushParentMethodName, true, false, false)
                .Generator
                .LdThisFld(ParentListField)
                .Ldarg(1)
                .EmitCall(objectListType.FindMethod("Add", @void,
                    false, so))
                .Ldarg_0()
                .Ldarg(1)
                .Stfld(PropertyTargetObject)
                .Ret();

            var pop = builder.DefineMethod(@void, new IXamlXType[0], PopParentMethodName, true, false, false)
                .Generator;

            var idx = pop.DefineLocal(ts.GetType("System.Int32"));
            pop
                // var idx = _parents.Count - 1;
                .LdThisFld(ParentListField)
                .EmitCall(objectListType.FindMethod(m => m.Name == "get_Count"))
                .Ldc_I4(1).Emit(OpCodes.Sub).Stloc(idx)
                // this.PropertyTargetObject = _parents[idx];
                .Ldarg_0()
                .LdThisFld(ParentListField)
                .Ldloc(idx)
                .EmitCall(objectListType.FindMethod(m => m.Name == "get_Item"))
                .Stfld(PropertyTargetObject)
                // _parents.RemoveAt(idx);
                .LdThisFld(ParentListField)
                .Ldloc(idx).EmitCall(objectListType.FindMethod(m => m.Name == "RemoveAt"))
                .Ret();

        }
        
        private IXamlXMethodBuilder ImplementInterfacePropertyGetter(IXamlXTypeBuilder builder ,
            IXamlXType type, string name)
        {
            var prefix = type.Namespace + "." + type.Name + ".";
            var originalGetter = type.FindMethod(m => m.Name == "get_" + name);
            var gen = builder.DefineMethod(originalGetter.ReturnType, new IXamlXType[0],
                prefix + "get_" + name, false, false,
                true, originalGetter);
            builder.DefineProperty(originalGetter.ReturnType,prefix+ name, null, gen);
            return gen;
        }
        
        IXamlXType EmitTypeDescriptorContextStub(IXamlXTypeSystem typeSystem, IXamlXTypeBuilder builder,
            XamlXLanguageTypeMappings mappings)
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
                        typeSystem.FindType("System.NotSupportedException").FindConstructor(new List<IXamlXType>()))
                    .Emit(OpCodes.Throw);

            }
            MethodStub("OnComponentChanging");
            MethodStub("OnComponentChanged");

            return mappings.TypeDescriptorContext;
        }
        
        (IXamlXType type, IXamlXConstructor ctor, Action createCallback) EmitParentEnumerable(IXamlXTypeSystem typeSystem, IXamlXTypeBuilder parentBuilder,
            XamlXLanguageTypeMappings mappings)
        {
            var so = typeSystem.GetType("System.Object");
            var enumerableBuilder =
                parentBuilder.DefineSubType(typeSystem.GetType("System.Object"), "ParentStackEnumerable", false);
            var enumerableType = typeSystem.GetType("System.Collections.Generic.IEnumerable`1")
                .MakeGenericType(new[] {typeSystem.GetType("System.Object")});
            enumerableBuilder.AddInterfaceImplementation(enumerableType);

            var enumerableParentList =
                enumerableBuilder.DefineField(ParentListField.FieldType, "_parentList", false, false);
            var enumerableParentProvider =
                enumerableBuilder.DefineField(mappings.ServiceProvider, "_parentSP", false, false);
            
            var enumerableCtor = enumerableBuilder.DefineConstructor(false, enumerableParentList.FieldType, enumerableParentProvider.FieldType);
            enumerableCtor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
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

            var enumeratorBuilder = enumerableBuilder.DefineSubType(so, "Enumerator", true);
            enumeratorBuilder.AddInterfaceImplementation(enumeratorObjectType);


            
            var state = enumeratorBuilder.DefineField(typeSystem.GetType("System.Int32"), "_state", false, false);
            
            
            var parentList =
                enumeratorBuilder.DefineField(ParentListField.FieldType, "_parentList", false, false);
            var parentProvider =
                enumeratorBuilder.DefineField(mappings.ServiceProvider, "_parentSP", false, false);
            var listType = typeSystem.GetType("System.Collections.Generic.List`1")
                .MakeGenericType(new[] {so});
            var list = enumeratorBuilder.DefineField(listType, "_list", false, false);
            var listIndex = enumeratorBuilder.DefineField(typeSystem.GetType("System.Int32"), "_listIndex", false, false);
            var current = enumeratorBuilder.DefineField(so, "_current", false, false);
            var parentEnumerator = enumeratorBuilder.DefineField(enumeratorObjectType, "_parentEnumerator", false, false);
            var enumeratorCtor = enumeratorBuilder.DefineConstructor(false, parentList.FieldType, parentProvider.FieldType);
                enumeratorCtor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, parentList)
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_2)
                .Emit(OpCodes.Stfld, parentProvider)
                .Emit(OpCodes.Ret);
            
            var currentGetter = enumeratorBuilder.DefineMethod(so, new IXamlXType[0],
                "get_Current", true, false, true);
            enumeratorBuilder.DefineProperty(so, "Current", null, currentGetter);
            currentGetter.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldfld, current)
                .Emit(OpCodes.Ret);

            enumeratorBuilder.DefineMethod(typeSystem.FindType("System.Void"), new IXamlXType[0], "Reset", true, false,
                    true).Generator
                .Emit(OpCodes.Newobj,
                    typeSystem.FindType("System.NotSupportedException").FindConstructor(new List<IXamlXType>()))
                .Emit(OpCodes.Throw);
            
            var disposeGen = enumeratorBuilder.DefineMethod(typeSystem.FindType("System.Void"), new IXamlXType[0], 
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
            var moveNext = enumeratorBuilder.DefineMethod(boolType, new IXamlXType[0],
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
                .Ldarg_0().Dup().Dup().Ldfld(parentList).Stfld(list).LdThisFld(list)
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
                // if(parent._parentServiceProvider == null) goto eof;
                .LdThisFld(parentProvider).Brfalse(eof)
                // parentProv = (IParentStackProvider)parent.GetService(typeof(IParentStackProvider));
                .LdThisFld(parentProvider).Ldtype(mappings.ParentStackProvider)
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
                
            var createEnumerator = enumerableBuilder.DefineMethod(enumeratorObjectType, new IXamlXType[0], "GetEnumerator", true, false,
                    true);
            createEnumerator.Generator
                .LdThisFld(enumerableParentList)
                .LdThisFld(enumerableParentProvider)
                .Emit(OpCodes.Newobj, enumeratorCtor)
                .Emit(OpCodes.Ret);

            enumerableBuilder.DefineMethod(enumeratorType, new IXamlXType[0],
                    "System.Collections.IEnumerable.GetEnumerator", false, false, true,
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
