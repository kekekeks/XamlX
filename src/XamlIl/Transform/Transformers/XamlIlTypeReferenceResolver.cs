using System;
using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlTypeReferenceResolver : IXamlIlAstTransformer
    {
        IXamlIlType ResolveType(XamlIlAstTransformationContext context,
            string xmlns, string name, List<XamlIlAstXmlTypeReference> typeArguments, IXamlIlLineInfo lineInfo)
        {
            var targs = typeArguments
                .Select(ta => ResolveType(context, ta.XmlNamespace, ta.Name, ta.GenericArguments, lineInfo))
                .ToList();
            
                
            const string clrNamespace = "clr-namespace:";
            const string assemblyNamePrefix = ";assembly=";

            IXamlIlType Attempt(Func<string, IXamlIlType> cb, string xname)
            {
                var suffix = (typeArguments.Count != 0) ? ("`" + typeArguments.Count) : "";
                return cb(xname + suffix) ?? cb(xname + "Extension" + suffix);
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
            if (context.StrictMode)
                throw new XamlIlParseException(
                    $"Unable to resolve type {name} from namespace {xmlns}", lineInfo);
            return null;
        }

        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstXmlTypeReference xmlref)
            {
                var type = ResolveType(context, xmlref.XmlNamespace, xmlref.Name, xmlref.GenericArguments, xmlref);
                return new XamlIlAstClrTypeReference(xmlref, type);
            }
            return node;
        }
    }
}