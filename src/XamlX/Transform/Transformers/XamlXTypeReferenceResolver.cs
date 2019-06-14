using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXTypeReferenceResolver : IXamlXAstTransformer
    {
        public static XamlXAstClrTypeReference ResolveType(XamlXAstTransformationContext context,
            string xmlns, string name, bool isMarkupExtension, List<XamlXAstXmlTypeReference> typeArguments, IXamlXLineInfo lineInfo,
            bool strict)
        {
            if (typeArguments == null)
                typeArguments = new List<XamlXAstXmlTypeReference>();
            var targs = typeArguments
                .Select(ta =>
                    ResolveType(context, ta.XmlNamespace, ta.Name, false, ta.GenericArguments, lineInfo, strict)
                        ?.Type)
                .ToList();
            
            IXamlXType Attempt(Func<string, IXamlXType> cb, string xname)
            {
                var suffix = (typeArguments.Count != 0) ? ("`" + typeArguments.Count) : "";
                if (isMarkupExtension)
                    return cb(xname + "Extension" + suffix) ?? cb(xname + suffix);
                else
                    return cb(xname + suffix) ?? cb(xname + "Extension" + suffix);
            }
            
            IXamlXType found = null;
            
            // Try to resolve from system
            if (xmlns == XamlNamespaces.Xaml2006)
                found = context.Configuration.TypeSystem.FindType("System." + name);


            if (found == null)
            {
                var resolvedNamespaces = XamlXNamespaceInfoHelper.TryResolve(context.Configuration, xmlns);
                if (resolvedNamespaces?.Count > 0)
                    found = Attempt(formedName =>
                    {
                        foreach (var resolvedNs in resolvedNamespaces)
                        {
                            var rname = resolvedNs.ClrNamespace + "." + formedName;
                            IXamlXType subRes;
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
                return new XamlXAstClrTypeReference(lineInfo, found,
                    isMarkupExtension || found.Name.EndsWith("Extension")); 
            if (strict)
                throw new XamlXParseException(
                    $"Unable to resolve type {name} from namespace {xmlns}", lineInfo);
            return null;
        }

        public static XamlXAstClrTypeReference ResolveType(XamlXAstTransformationContext context,
            string xmlName, bool isMarkupExtension, IXamlXLineInfo lineInfo,
            bool strict)
        {
            var pair = xmlName.Split(new[] {':'}, 2);
            var (shortNs, name) = pair.Length == 1 ? ("", pair[0]) : (pair[0], pair[1]);
            if (!context.NamespaceAliases.TryGetValue(shortNs, out var xmlns))
            {
                if (strict)
                    throw new XamlXParseException(
                        $"Unable to resolve type namespace alias {shortNs}", lineInfo);
                return null;
            }

            return ResolveType(context, xmlns, name, isMarkupExtension, new List<XamlXAstXmlTypeReference>(), lineInfo, strict);
        }

        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstXmlTypeReference xmlref)
            {
                var resolved = ResolveType(context, xmlref, context.StrictMode);
                if (resolved == null)
                    return node;
                return resolved;
            }

            return node;
        }

        public static XamlXAstClrTypeReference ResolveType(XamlXAstTransformationContext context,
            XamlXAstXmlTypeReference xmlref, bool strict)
        {
            return ResolveType(context, xmlref.XmlNamespace, xmlref.Name, xmlref.IsMarkupExtension,
                xmlref.GenericArguments, xmlref, strict);

        }
    }
}
