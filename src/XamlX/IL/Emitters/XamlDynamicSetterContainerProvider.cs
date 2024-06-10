using XamlX.Ast;
using XamlX.Emit;
using XamlX.TypeSystem;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlDynamicSetterContainerProvider
    {
        IXamlDynamicSetterContainer ProvideDynamicSetterContainer(
            XamlAstClrProperty property,
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context);
    }

#if !XAMLX_INTERNAL
    public
#endif
    class DefaultXamlDynamicSetterContainerProvider : IXamlDynamicSetterContainerProvider
    {
        private readonly IXamlDynamicSetterContainer? _sharedContainer;

        public DefaultXamlDynamicSetterContainerProvider(IXamlTypeBuilder<IXamlILEmitter>? sharedContainerType)
            => _sharedContainer = sharedContainerType is null
                ? null
                : new DefaultXamlDynamicSetterContainer(sharedContainerType, XamlVisibility.Public);

        public IXamlDynamicSetterContainer ProvideDynamicSetterContainer(
            XamlAstClrProperty property,
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context)
            => _sharedContainer is null || (property.IsPrivate || property.IsFamily) || IsTypeEffectivelyPrivate(property.DeclaringType)
                ? GetOrCreatePrivateContainer(context)
                : _sharedContainer;

        private static bool IsTypeEffectivelyPrivate(IXamlType? xamlType)
        {
            for (var type = xamlType; type is not null; type = type.DeclaringType)
            {
                if (type.IsNestedPrivate)
                    return true;
            }

            return false;
        }

        private static DefaultXamlDynamicSetterContainer GetOrCreatePrivateContainer(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context)
        {
            if (!context.TryGetItem<DefaultXamlDynamicSetterContainer>(out var container))
            {
                container = new DefaultXamlDynamicSetterContainer(context.DeclaringType, XamlVisibility.Private);
                context.SetItem(container);
            }

            return container;
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlDynamicSetterContainer
    {
        IXamlTypeBuilder<IXamlILEmitter> TypeBuilder { get; }
        XamlVisibility GeneratedMethodsVisibility { get; }
        string GetDynamicSetterMethodName(int setterIndex);
    }

#if !XAMLX_INTERNAL
    public
#endif
    class DefaultXamlDynamicSetterContainer : IXamlDynamicSetterContainer
    {
        public IXamlTypeBuilder<IXamlILEmitter> TypeBuilder { get; }

        public XamlVisibility GeneratedMethodsVisibility { get; }

        public string GetDynamicSetterMethodName(int setterIndex)
            => "<>XamlDynamicSetter_" + (setterIndex + 1);

        public DefaultXamlDynamicSetterContainer(
            IXamlTypeBuilder<IXamlILEmitter> typeBuilder,
            XamlVisibility generatedMethodsVisibility)
        {
            TypeBuilder = typeBuilder;
            GeneratedMethodsVisibility = generatedMethodsVisibility;
        }
    }

}
