using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlTypeReferenceResolver : IXamlAstTransformer
    {
        public static IXamlType ResolveType(XamlAstTransformationContext context,
            string xmlns, string name, List<XamlAstXmlTypeReference> typeArguments, IXamlLineInfo lineInfo,
            bool strict)
        {
            var targs = typeArguments
                .Select(ta => ResolveType(context, ta.XmlNamespace, ta.Name, ta.GenericArguments, lineInfo, strict))
                .ToList();
            
            IXamlType Attempt(Func<string, IXamlType> cb, string xname)
            {
                var suffix = (typeArguments.Count != 0) ? ("`" + typeArguments.Count) : "";
                return cb(xname + "Extension" + suffix) ?? cb(xname + suffix);
            }
            
            IXamlType found = null;
            
            // Try to resolve from system
            if (xmlns == XamlNamespaces.Xaml2006)
                found = context.Configuration.TypeSystem.FindType("System." + name);


            if (found == null)
            {
                var resolvedNamespaces = XamlNamespaceInfoHelper.TryResolve(context.Configuration, xmlns);
                if (resolvedNamespaces?.Count > 0)
                    foreach (var resolvedNs in resolvedNamespaces)
                    {
                        var rname = resolvedNs.ClrNamespace + "." + name;
                        if (resolvedNs.Assembly != null)
                            found = Attempt(resolvedNs.Assembly.FindType, rname);
                        else
                            found = Attempt(x =>
                                context.Configuration.TypeSystem.FindType(x, resolvedNs.AssemblyName), rname);
                        if (found != null)
                            break;
                    }
            }

            if (typeArguments.Count != 0)
                found = found?.MakeGenericType(targs);
            if (found != null)
                return found;
            if (strict)
                throw new XamlParseException(
                    $"Unable to resolve type {name} from namespace {xmlns}", lineInfo);
            return null;
        }

        public static IXamlType ResolveType(XamlAstTransformationContext context,
            string xmlName, IXamlLineInfo lineInfo,
            bool strict)
        {
            var pair = xmlName.Split(new[] {':'}, 2);
            var (shortNs, name) = pair.Length == 1 ? ("", pair[0]) : (pair[0], pair[1]);
            if (!context.NamespaceAliases.TryGetValue(shortNs, out var xmlns))
            {
                if (strict)
                    throw new XamlParseException(
                        $"Unable to resolve type namespace alias {shortNs}", lineInfo);
                return null;
            }

            return ResolveType(context, xmlns, name, new List<XamlAstXmlTypeReference>(), lineInfo, strict);
        }

        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXmlTypeReference xmlref)
            {
                var type = ResolveType(context, xmlref, context.StrictMode);
                if (type == null)
                    return node;
                return new XamlAstClrTypeReference(node, type);
            }

            return node;
        }

        public static IXamlType ResolveType(XamlAstTransformationContext context,
            XamlAstXmlTypeReference xmlref, bool strict)
        {
            return ResolveType(context, xmlref.XmlNamespace, xmlref.Name, xmlref.GenericArguments, xmlref, strict);

        }
    }
}
