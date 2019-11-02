using System;
using System.Collections.Generic;
using System.Text;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlILLanguageTypeMappings : Transform.XamlLanguageTypeMappings
    {
        public XamlILLanguageTypeMappings(IXamlTypeSystem typeSystem) : base(typeSystem)
        {
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    delegate void XamlContextTypeBuilderCallback(IXamlTypeBuilder typeBuilder, IXamlILEmitter constructor);

#if !XAMLX_INTERNAL
    public
#endif
    delegate void XamlContextFactoryCallback(XamlContext context, IXamlILEmitter emitter);

}
