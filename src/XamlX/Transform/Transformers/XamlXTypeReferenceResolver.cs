using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlXTypeReferenceResolver : IXamlXAstTransformer
    {
        IXamlXType ResolveType(XamlXAstTransformationContext context,
            string xmlns, string name, List<XamlXAstXmlTypeReference> typeArguments, IXamlXLineInfo lineInfo)
        {
            var targs = typeArguments
                .Select(ta => ResolveType(context, ta.XmlNamespace, ta.Name, ta.GenericArguments, lineInfo))
                .ToList();
            
                
            const string clrNamespace = "clr-namespace:";
            const string assemblyNamePrefix = ";assembly=";

            IXamlXType Attempt(Func<string, IXamlXType> cb, string xname)
            {
                var suffix = (typeArguments.Count != 0) ? ("`" + typeArguments.Count) : "";
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
            if (context.StrictMode)
                throw new XamlXParseException(
                    $"Unable to resolve type {name} from namespace {xmlns}", lineInfo);
            return null;
        }

        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstXmlTypeReference xmlref)
            {
                var type = ResolveType(context, xmlref.XmlNamespace, xmlref.Name, xmlref.GenericArguments, xmlref);
                return new XamlXAstClrTypeReference(xmlref, type);
            }
            return node;
        }
    }
}