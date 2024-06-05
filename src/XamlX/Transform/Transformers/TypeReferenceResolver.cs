using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;
namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class TypeReferenceResolver : IXamlAstTransformer
    {
        class TypeResolverCache
        {
            public Dictionary<(string?, string, bool), IXamlType> CacheDictionary = new();
        }
        
        public static XamlAstClrTypeReference ResolveType(AstTransformationContext context,
            string? xmlns, string name, bool isMarkupExtension, List<XamlAstXmlTypeReference>? typeArguments,
            IXamlLineInfo lineInfo)
        {
            if (typeArguments == null || typeArguments.Count == 0)
            {
                var cache = context.GetOrCreateItem<TypeResolverCache>();
                var cacheKey = (xmlns, name, isMarkupExtension);
                if (cache.CacheDictionary.TryGetValue(cacheKey, out var type))
                {
                    return new XamlAstClrTypeReference(lineInfo, type, isMarkupExtension);
                }

                var res = ResolveTypeCore(context, xmlns, name, isMarkupExtension, typeArguments, lineInfo);
                cache.CacheDictionary[cacheKey] = res.Type;
                return res;

            }
            else
                return ResolveTypeCore(context, xmlns, name, isMarkupExtension, typeArguments, lineInfo);
        }

        static XamlAstClrTypeReference ResolveTypeCore(AstTransformationContext context,
            string? xmlns, string name, bool isMarkupExtension, List<XamlAstXmlTypeReference>? typeArguments, IXamlLineInfo lineInfo)
        {
            if (typeArguments == null)
                typeArguments = new List<XamlAstXmlTypeReference>();
            var targs = typeArguments
                .Select(ta =>
                    ResolveType(context, ta.XmlNamespace, ta.Name, false, ta.GenericArguments, lineInfo)
                        .Type)
                .ToList();
            
            IXamlType? Attempt(Func<string, IXamlType?> cb, string xname)
            {
                var suffix = (typeArguments.Count != 0) ? ("`" + typeArguments.Count) : "";
                if (isMarkupExtension)
                    return cb(xname + "Extension" + suffix) ?? cb(xname + suffix);
                else
                    return cb(xname + suffix) ?? cb(xname + "Extension" + suffix);
            }
            
            IXamlType? found = null;
            
            // Try to resolve from system
            if (xmlns == XamlNamespaces.Xaml2006)
                found = context.Configuration.TypeSystem.FindType("System." + name);


            if (found == null)
            {
                var resolvedNamespaces = NamespaceInfoHelper.TryResolve(context.Configuration, xmlns);
                if (resolvedNamespaces?.Count > 0)
                    found = Attempt(formedName =>
                    {
                        foreach (var resolvedNs in resolvedNamespaces)
                        {
                            var rname = resolvedNs.ClrNamespace + "." + formedName;
                            IXamlType? subRes = null;
                            if (resolvedNs.Assembly != null)
                                subRes = resolvedNs.Assembly.FindType(rname);
                            else if (resolvedNs.AssemblyName != null)
                                subRes = context.Configuration.TypeSystem.FindType(rname, resolvedNs.AssemblyName);
                            else
                            {
                                foreach (var assembly in context.Configuration.TypeSystem.Assemblies)
                                {
                                    subRes = assembly.FindType(rname);
                                    if (subRes != null)
                                        break;
                                }
                            }
                            if (subRes != null)
                                return subRes;
                        }

                        return null;
                    }, name);
            }

            if (typeArguments.Count != 0)
                found = found?.MakeGenericType(targs);
            if (found != null)
                return new XamlAstClrTypeReference(lineInfo, found,
                    isMarkupExtension || found.Name.EndsWith("Extension"));

            throw new XamlTransformException($"Unable to resolve type {name} from namespace {xmlns}", lineInfo);
        }

        public static XamlAstClrTypeReference ResolveType(AstTransformationContext context,
            string xmlName, bool isMarkupExtension, IXamlLineInfo lineInfo,
            bool strict)
        {
            var pair = xmlName.Split(new[] {':'}, 2);
            var (shortNs, name) = pair.Length == 1 ? ("", pair[0]) : (pair[0], pair[1]);
            if (!context.NamespaceAliases.TryGetValue(shortNs, out var xmlns))
            {
                throw new XamlTransformException($"Unable to resolve type namespace alias {shortNs}", lineInfo);
            }

            return ResolveType(context, xmlns, name, isMarkupExtension, new List<XamlAstXmlTypeReference>(), lineInfo);
        }

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXmlTypeReference xmlref)
            {
                var resolved = ResolveType(context, xmlref);
                return resolved;
            }

            return node;
        }

        public static XamlAstClrTypeReference ResolveType(AstTransformationContext context,
            XamlAstXmlTypeReference xmlref)
        {
            return ResolveType(context, xmlref.XmlNamespace, xmlref.Name, xmlref.IsMarkupExtension,
                xmlref.GenericArguments, xmlref);

        }
    }
}
