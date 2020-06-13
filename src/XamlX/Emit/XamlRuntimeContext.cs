using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XamlX.TypeSystem;

namespace XamlX.Emit
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlRuntimeContext<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        public IXamlField RootObjectField { get; set; }
        public IXamlField IntermediateRootObjectField { get; set; }
        public IXamlField ParentListField { get; set; }
        public IXamlType ContextType { get; set; }
        public IXamlField PropertyTargetObject { get; set; }
        public IXamlField PropertyTargetProperty { get; set; }
        public IXamlConstructor Constructor { get; set; }
        public Action<TBackendEmitter> Factory { get; set; }
        public IXamlMethod PushParentMethod { get; set; }
        public IXamlMethod PopParentMethod { get; set; }

        public XamlRuntimeContext(IXamlType definition, IXamlType constructedType,
            XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> mappings,
            Action<XamlRuntimeContext<TBackendEmitter, TEmitResult>, TBackendEmitter> factory)
        {
            ContextType = definition.MakeGenericType(constructedType);

            IXamlField Get(string s) =>
                ContextType.Fields.FirstOrDefault(f => f.Name == s);

            IXamlMethod GetMethod(string s) =>
                ContextType.Methods.FirstOrDefault(f => f.Name == s);

            RootObjectField = Get(XamlRuntimeContextDefintion.RootObjectFieldName);
            IntermediateRootObjectField = Get(XamlRuntimeContextDefintion.IntermediateRootObjectFieldName);
            ParentListField = Get(XamlRuntimeContextDefintion.ParentListFieldName);
            PropertyTargetObject = Get(XamlRuntimeContextDefintion.ProvideTargetObjectName);
            PropertyTargetProperty = Get(XamlRuntimeContextDefintion.ProvideTargetPropertyName);
            PushParentMethod = GetMethod(XamlRuntimeContextDefintion.PushParentMethodName);
            PopParentMethod = GetMethod(XamlRuntimeContextDefintion.PopParentMethodName);
            Constructor = ContextType.Constructors.First();
            Factory = il => factory(this, il);
            if (mappings.ContextFactoryCallback != null)
                Factory = il =>
                {
                    factory(this, il);
                    mappings.ContextFactoryCallback(this, il);
                };
        }
    }

    static class XamlRuntimeContextDefintion
    {
        public const string RootObjectFieldName = "RootObject";
        public const string IntermediateRootObjectFieldName = "IntermediateRoot";
        public const string ParentListFieldName = "ParentsStack";
        public const string ProvideTargetObjectName = "ProvideTargetObject";
        public const string ProvideTargetPropertyName = "ProvideTargetProperty";
        public const string PushParentMethodName = "PushParent";
        public const string PopParentMethodName = "PopParent";
    }
}
