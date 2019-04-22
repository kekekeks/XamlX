using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlTypeReferenceResolver : IXamlAstTransformer
    {
        public static XamlAstClrTypeReference ResolveType(XamlAstTransformationContext context,
            string xmlns, string name, bool isMarkupExtension, List<XamlAstXmlTypeReference> typeArguments, IXamlLineInfo lineInfo,
            bool strict)
        {
            var targs = typeArguments
                .Select(ta =>
                    ResolveType(context, ta.XmlNamespace, ta.Name, false, ta.GenericArguments, lineInfo, strict)
                        ?.Type)
                .ToList();
            
            IXamlType Attempt(Func<string, IXamlType> cb, string xname)
            {
                var suffix = (typeArguments.Count != 0) ? ("`" + typeArguments.Count) : "";
                IXamlType ares = null;
                if (isMarkupExtension)
                    ares = cb(xname + "Extension" + suffix);
                return ares ?? cb(xname + suffix);
            }
            
            IXamlType found = null;
            
            // Try to resolve from system
            if (xmlns == XamlNamespaces.Xaml2006)
                found = context.Configuration.TypeSystem.FindType("System." + name);


            if (found == null)
            {
                var resolvedNamespaces = XamlNamespaceInfoHelper.TryResolve(context.Configuration, xmlns);
                if (resolvedNamespaces?.Count > 0)
                    found = Attempt(formedName =>
                    {
                        foreach (var resolvedNs in resolvedNamespaces)
                        {
                            var rname = resolvedNs.ClrNamespace + "." + formedName;
                            IXamlType subRes;
                            if (resolvedNs.Assembly != null)
                                subRes = resolvedNs.Assembly.FindType(rname);
                            else
                                subRes = context.Configuration.TypeSystem.FindType(rname, resolvedNs.AssemblyName);
                            if (subRes != null)
                                return subRes;
                        }

                        return null;
                    }, name);
            }

            if (typeArguments.Count != 0)
                found = found?.MakeGenericType(targs);
            if (found != null)
                return new XamlAstClrTypeReference(lineInfo, found, isMarkupExtension); 
            if (strict)
                throw new XamlParseException(
                    $"Unable to resolve type {name} from namespace {xmlns}", lineInfo);
            return null;
        }

        public static XamlAstClrTypeReference ResolveType(XamlAstTransformationContext context,
            string xmlName, bool isMarkupExtension, IXamlLineInfo lineInfo,
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

            return ResolveType(context, xmlns, name, isMarkupExtension, new List<XamlAstXmlTypeReference>(), lineInfo, strict);
        }

        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXmlTypeReference xmlref)
            {
                var resolved = ResolveType(context, xmlref, context.StrictMode);
                if (resolved == null)
                    return node;
                return resolved;
            }

            return node;
        }

        public static XamlAstClrTypeReference ResolveType(XamlAstTransformationContext context,
            XamlAstXmlTypeReference xmlref, bool strict)
        {
            return ResolveType(context, xmlref.XmlNamespace, xmlref.Name, xmlref.IsMarkupExtension,
                xmlref.GenericArguments, xmlref, strict);

        }
    }
}