using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlXTypeReferenceResolver : IXamlXAstTransformer
    {
        public static IXamlXType ResolveType(XamlXAstTransformationContext context,
            string xmlns, string name, List<XamlXAstXmlTypeReference> typeArguments, IXamlXLineInfo lineInfo,
            bool strict)
        {
            var targs = typeArguments
                .Select(ta => ResolveType(context, ta.XmlNamespace, ta.Name, ta.GenericArguments, lineInfo, strict))
                .ToList();
            
            IXamlXType Attempt(Func<string, IXamlXType> cb, string xname)
            {
                var suffix = (typeArguments.Count != 0) ? ("`" + typeArguments.Count) : "";
                return cb(xname + "Extension" + suffix) ?? cb(xname + suffix);
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
                return found;
            if (strict)
                throw new XamlXParseException(
                    $"Unable to resolve type {name} from namespace {xmlns}", lineInfo);
            return null;
        }

        public static IXamlXType ResolveType(XamlXAstTransformationContext context,
            string xmlName, IXamlXLineInfo lineInfo,
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

            return ResolveType(context, xmlns, name, new List<XamlXAstXmlTypeReference>(), lineInfo, strict);
        }

        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstXmlTypeReference xmlref)
            {
                var type = ResolveType(context, xmlref, context.StrictMode);
                if (type == null)
                    return node;
                return new XamlXAstClrTypeReference(node, type);
            }

            return node;
        }

        public static IXamlXType ResolveType(XamlXAstTransformationContext context,
            XamlXAstXmlTypeReference xmlref, bool strict)
        {
            return ResolveType(context, xmlref.XmlNamespace, xmlref.Name, xmlref.GenericArguments, xmlref, strict);

        }
    }
}
