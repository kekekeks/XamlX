using System;
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
        var experimentalAttributeType = context.Configuration.WellKnownTypes.ExperimentalAttribute;

        if (node is XamlAstObjectNode ctorNode
            && FindAttr(ctorNode.Type.GetClrType().CustomAttributes) is var (typeAttr, typeDiagnostic))
        {
            var type = ctorNode.Type.GetClrType();
            Report(type.Name, typeAttr, typeDiagnostic);
        }
        else if (node is XamlAstXamlPropertyValueNode propNode
                 && FindAttr(propNode.Property.GetClrProperty().CustomAttributes) is var (propAttr, propDiagnostic))
        {
            var prop = propNode.Property.GetClrProperty();
            Report($"{prop.DeclaringType.Name}.{prop.Name}", propAttr, propDiagnostic);
        }
        else if (node is XamlStaticExtensionNode staticExt)
        {
            var member = staticExt.ResolveMember(false);
            var result = member switch
            {
                IXamlField field => FindAttr(field.CustomAttributes),
                IXamlProperty { Getter: not null } prop => FindAttr(prop.CustomAttributes.Concat(prop.Getter.CustomAttributes)),
                _ => null
            };
            if (result is var (staticAttr, staticDiagnostic))
            {
                Report($"{member!.DeclaringType.Name}.{member.Name}", staticAttr, staticDiagnostic);
            }
        }
        
        return node;

        (IXamlCustomAttribute Attribute, DiagnosticType DiagnosticType)? FindAttr(IEnumerable<IXamlCustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Type.Equals(obsoleteAttributeType))
                    return (attribute, DiagnosticType.Obsolete);
                if (experimentalAttributeType is not null && attribute.Type.Equals(experimentalAttributeType))
                    return (attribute, DiagnosticType.Experimental);
            }

            return null;
        }

        void Report(string member, IXamlCustomAttribute attribute, DiagnosticType diagnosticType)
        {
            switch (diagnosticType)
            {
                case DiagnosticType.Obsolete:
                    ReportObsolete(member, attribute);
                    break;
                case DiagnosticType.Experimental:
                    ReportExperimental(member, attribute);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(diagnosticType), diagnosticType, null);
            }
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

        void ReportExperimental(string member, IXamlCustomAttribute attribute)
        {
            if (attribute.Parameters.FirstOrDefault() is not string diagnosticId)
                return;

            attribute.Properties.TryGetValue("Message", out var messageObject);
            var message = messageObject as string;

            var title = string.IsNullOrEmpty(message) ?
                $"'{member}' is for evaluation purposes only and is subject to change or removal in future updates." :
                $"'{member}' is for evaluation purposes only and is subject to change or removal in future updates: '{message}'.";

            var code = context.Configuration.DiagnosticsHandler.CodeMappings(diagnosticId);
            context.ReportDiagnostic(code, XamlDiagnosticSeverity.Warning, title, node);
        }
    }

    private enum DiagnosticType
    {
        Obsolete,
        Experimental
    }
}