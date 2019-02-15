using System;
using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlTypeReferenceResolver : IXamlIlAstTransformer
    {
        public static IXamlIlType ResolveType(XamlIlAstTransformationContext context,
            string xmlns, string name, List<XamlIlAstXmlTypeReference> typeArguments, IXamlIlLineInfo lineInfo,
            bool strict)
        {
            var targs = typeArguments
                .Select(ta => ResolveType(context, ta.XmlNamespace, ta.Name, ta.GenericArguments, lineInfo, strict))
                .ToList();
            
            IXamlIlType Attempt(Func<string, IXamlIlType> cb, string xname)
            {
                var suffix = (typeArguments.Count != 0) ? ("`" + typeArguments.Count) : "";
                return cb(xname + "Extension" + suffix) ?? cb(xname + suffix);
            }
            
            IXamlIlType found = null;
            
            // Try to resolve from system
            if (xmlns == XamlNamespaces.Xaml2006)
                found = context.Configuration.TypeSystem.FindType("System." + name);


            if (found == null)
            {
                var resolvedNamespaces = XamlIlNamespaceInfoHelper.TryResolve(context.Configuration, xmlns);
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
                throw new XamlIlParseException(
                    $"Unable to resolve type {name} from namespace {xmlns}", lineInfo);
            return null;
        }

        public static IXamlIlType ResolveType(XamlIlAstTransformationContext context,
            string xmlName, IXamlIlLineInfo lineInfo,
            bool strict)
        {
            var pair = xmlName.Split(new[] {':'}, 2);
            var (shortNs, name) = pair.Length == 1 ? ("", pair[0]) : (pair[0], pair[1]);
            if (!context.NamespaceAliases.TryGetValue(shortNs, out var xmlns))
            {
                if (strict)
                    throw new XamlIlParseException(
                        $"Unable to resolve type namespace alias {shortNs}", lineInfo);
                return null;
            }

            return ResolveType(context, xmlns, name, new List<XamlIlAstXmlTypeReference>(), lineInfo, strict);
        }

        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstXmlTypeReference xmlref)
            {
                var type = ResolveType(context, xmlref, context.StrictMode);
                if (type == null)
                    return node;
                return new XamlIlAstClrTypeReference(node, type);
            }

            return node;
        }

        public static IXamlIlType ResolveType(XamlIlAstTransformationContext context,
            XamlIlAstXmlTypeReference xmlref, bool strict)
        {
            return ResolveType(context, xmlref.XmlNamespace, xmlref.Name, xmlref.GenericArguments, xmlref, strict);

        }
    }
}
