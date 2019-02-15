using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlTypeReferenceResolver : IXamlAstTransformer
    {
        IXamlType ResolveType(XamlAstTransformationContext context,
            string xmlns, string name, List<XamlAstXmlTypeReference> typeArguments, IXamlLineInfo lineInfo)
        {
            var targs = typeArguments
                .Select(ta => ResolveType(context, ta.XmlNamespace, ta.Name, ta.GenericArguments, lineInfo))
                .ToList();
            
                
            const string clrNamespace = "clr-namespace:";
            const string assemblyNamePrefix = ";assembly=";

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
            if (context.StrictMode)
                throw new XamlParseException(
                    $"Unable to resolve type {name} from namespace {xmlns}", lineInfo);
            return null;
        }

        public IXamlAstNode Transform(XamlAstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXmlTypeReference xmlref)
            {
                var type = ResolveType(context, xmlref.XmlNamespace, xmlref.Name, xmlref.GenericArguments, xmlref);
                return new XamlAstClrTypeReference(xmlref, type);
            }
            return node;
        }
    }
}