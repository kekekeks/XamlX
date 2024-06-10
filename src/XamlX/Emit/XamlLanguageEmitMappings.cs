using System;
using XamlX.Ast;
using XamlX.IL;

namespace XamlX.Emit
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlLanguageEmitMappings<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        public Func<XamlEmitContext<TBackendEmitter, TEmitResult>, TBackendEmitter, XamlAstClrProperty, bool>? ProvideValueTargetPropertyEmitter { get; set; }
        public XamlContextTypeBuilderCallback<TBackendEmitter>? ContextTypeBuilderCallback { get; set; }
        public XamlContextFactoryCallback<TBackendEmitter, TEmitResult>? ContextFactoryCallback { get; set; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    delegate void XamlContextTypeBuilderCallback<TBackendEmitter>(IXamlILContextDefinition<TBackendEmitter> contextDefinition);

#if !XAMLX_INTERNAL
    public
#endif
    delegate void XamlContextFactoryCallback<TBackendEmitter, TEmitResult>(XamlRuntimeContext<TBackendEmitter, TEmitResult> context, TBackendEmitter emitter)
        where TEmitResult : IXamlEmitResult;
}
