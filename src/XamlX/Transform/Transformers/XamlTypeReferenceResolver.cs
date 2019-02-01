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
            if (typeArguments.Count != 0)
                name += "`" + typeArguments.Count;
                
            const string clrNamespace = "clr-namespace:";
            const string assemblyNamePrefix = ";assembly=";
            
            
            
            IXamlType found = null;
            
            // Try to resolve from system
            if (xmlns == XamlNamespaces.Xaml2006)
                found = context.Configuration.TypeSystem.FindType("System." + name);

            // Try registered xmlns
            if (found == null && context.Configuration.XmlnsMappings.Namespaces.TryGetValue(xmlns, out var lst))
            {
                foreach (var pair in lst)
                {
                    found = pair.asm.FindType(pair.ns + "." + name);
                    if (found != null)
                        break;
                }
            }

            // Try to resolve from clr-namespace
            if (found == null && xmlns.StartsWith(clrNamespace))
            {
                var ns = xmlns.Substring(clrNamespace.Length);

                // We are completely ignoring `;assembly=` part because of type forwarding shenanigans with
                // netstandard and .NET Core
                
                var indexOfAssemblyPrefix = ns.IndexOf(assemblyNamePrefix, StringComparison.Ordinal);
                if (indexOfAssemblyPrefix != -1) 
                    ns = ns.Substring(0, indexOfAssemblyPrefix);

                found = context.Configuration.TypeSystem.FindType($"{ns}.{name}");
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