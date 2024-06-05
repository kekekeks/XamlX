using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers;

#if !XAMLX_INTERNAL
public
#endif
    class ObsoleteWarningsTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        var obsoleteAttributeType = context.Configuration.WellKnownTypes.ObsoleteAttribute;

        if (node is XamlAstObjectNode ctorNode
            && FindAttr(ctorNode.Type.GetClrType().CustomAttributes) is { } typeAttr)
        {
            var type = ctorNode.Type.GetClrType();
            ReportObsolete(type.Name, typeAttr);
        }
        else if (node is XamlAstXamlPropertyValueNode propNode
                 && FindAttr(propNode.Property.GetClrProperty().CustomAttributes) is { } propAttr)
        {
            var prop = propNode.Property.GetClrProperty();
            ReportObsolete($"{prop.Name}.{prop.Name}", propAttr);
        }
        else if (node is XamlStaticExtensionNode staticExt)
        {
            var staticAttr = staticExt.ResolveMember(false) switch
            {
                IXamlField field => FindAttr(field.CustomAttributes),
                IXamlProperty { Getter: not null } prop => FindAttr(prop.CustomAttributes.Concat(prop.Getter.CustomAttributes)),
                _ => null
            };
            if (staticAttr is not null)
            {
                ReportObsolete($"{staticExt.Type.GetClrType().Name}.{staticExt.Member}", staticAttr);
            }
        }
        
        return node;

        IXamlCustomAttribute? FindAttr(IEnumerable<IXamlCustomAttribute> attributes)
        {
            return attributes.FirstOrDefault(a => a.Type.Equals(obsoleteAttributeType));
        }

        void ReportObsolete(string member, IXamlCustomAttribute attribute)
        {
            var title = $"'{member}' is obsolete";
            if (attribute.Parameters.FirstOrDefault() is {} description)
            {
                title += ": " + description;
            }
            var isError = attribute.Parameters.Skip(1).FirstOrDefault() as bool? ?? false;

            var code = context.Configuration.DiagnosticsHandler.CodeMappings(XamlXWellKnownDiagnosticCodes.Obsolete);
            context.ReportDiagnostic(
                code,
                isError ? XamlDiagnosticSeverity.Error : XamlDiagnosticSeverity.Warning,
                title, node);
        }
    }
}