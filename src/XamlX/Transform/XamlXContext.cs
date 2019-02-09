using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
    public class XamlXContext
    {
        public IXamlXField RootObjectField { get; set; }
        public IXamlXField ParentListField { get; set; }
        public IXamlXType ContextType { get; set; }
        private IXamlXField _parentStackEnumerableField;
        private IXamlXField _parentServiceProviderField;
        private IXamlXField _innerServiceProviderField;
        public IXamlXField PropertyTargetObject { get; set; }
        public IXamlXField PropertyTargetProperty { get; set; }

        /// <summary>
        /// Ctor expects IServiceProvider
        /// </summary>
        public IXamlXConstructor Constructor { get; set; }

        public static XamlXContext GenerateContextClass(IXamlXTypeBuilder builder,
            IXamlXTypeSystem typeSystem, XamlXLanguageTypeMappings mappings,
            IXamlXType rootType) => new XamlXContext(builder, typeSystem, mappings, rootType);
        
        
        private XamlXContext(IXamlXTypeBuilder builder, 
            IXamlXTypeSystem typeSystem, XamlXLanguageTypeMappings mappings,
            IXamlXType rootType)
        {
            RootObjectField = builder.DefineField(rootType, "RootObject", true, false);
            _parentServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_sp", false, false);
            if (mappings.InnerServiceProviderFactoryMethod != null)
                _innerServiceProviderField = builder.DefineField(mappings.ServiceProvider, "_innerSp", false, false);
            var so = typeSystem.GetType("System.Object");
            var systemType = typeSystem.GetType("System.Type");

            var ownServices = new List<IXamlXType>();
            var ctorCallbacks = new List<Action<IXamlXEmitter>>();
            var createCallbacks = new List<Action>();
            
            if (mappings.RootObjectProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.RootObjectProvider);
                ImplementInterfacePropertyGetter(builder, mappings.RootObjectProvider, "RootObject")
                    .Generator.LdThisFld(RootObjectField).Ret();
                ownServices.Add(mappings.RootObjectProvider);
            }

            if (mappings.ParentStackProvider != null)
            {
                builder.AddInterfaceImplementation(mappings.ParentStackProvider);
                var objectListType = typeSystem.GetType("System.Collections.Generic.List`1")
                    .MakeGenericType(new[] {typeSystem.GetType("System.Object")});
                ParentListField = builder.DefineField(objectListType, "ParentsStack", true, false);

                var enumerator = EmitParentEnumerable(typeSystem, builder, mappings);
                createCallbacks.Add(enumerator.createCallback);
                _parentStackEnumerableField = builder.DefineField(
                    typeSystem.GetType("System.Collections.Generic.IEnumerable`1").MakeGenericType(new[]{so}),
                    "_parentStackEnumerable", false, false);

                ImplementInterfacePropertyGetter(builder, mappings.ParentStackProvider, "Parents")
                    .Generator.LdThisFld(_parentStackEnumerableField).Ret();
                
                ctorCallbacks.Add(g => g
                    .Emit(OpCodes.Ldarg_0)
                    .Emit(OpCodes.Newobj, objectListType.FindConstructor(new List<IXamlXType>()))
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
            
            builder.AddInterfaceImplementation(mappings.ServiceProvider);
            var getServiceMethod = builder.DefineMethod(so,
                new[] {systemType},
                "GetService", true, false, true);

            ownServices = ownServices.Where(s => s != null).ToList();
            var getServiceInterfaceMethod = mappings.ServiceProvider.FindMethod("GetService", so, false, systemType);
            if (ownServices.Count != 0)
            {
                var compare = systemType.FindMethod("Equals", typeSystem.GetType("System.Boolean"),
                    false, systemType);
                var fromHandle = systemType.Methods.First(m => m.Name == "GetTypeFromHandle");

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
                .Emit(OpCodes.Ldfld, _parentServiceProviderField)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Callvirt, getServiceInterfaceMethod)
                .Emit(OpCodes.Ret);

            var ctor = builder.DefineConstructor(mappings.ServiceProvider);
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
            foreach (var cb in createCallbacks)
                cb();
            ContextType = builder.CreateType();
            
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

            var enumerableParent = enumerableBuilder.DefineField(parentBuilder, "_parent", false, false);
            var enumerableCtor = enumerableBuilder.DefineConstructor(parentBuilder);
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
            var enumeratorCtor = enumeratorBuilder.DefineConstructor(parentBuilder);
                enumeratorCtor.Generator
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Call, so.Constructors.First())
                .Emit(OpCodes.Ldarg_0)
                .Emit(OpCodes.Ldarg_1)
                .Emit(OpCodes.Stfld, parent)
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
                
            var createEnumerator = enumerableBuilder.DefineMethod(enumeratorObjectType, new IXamlXType[0], "GetEnumerator", true, false,
                    true);
            createEnumerator.Generator
                .Ldarg_0()
                .Ldfld(enumerableParent)
                .Emit(OpCodes.Newobj, enumeratorCtor)
                .Emit(OpCodes.Ret);

            enumerableBuilder.DefineMethod(enumeratorType, new IXamlXType[0],
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