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

            // Try registered xmlns
            if (found == null && context.Configuration.XmlnsMappings.Namespaces.TryGetValue(xmlns, out var lst))
            {
                foreach (var pair in lst)
                {
                    found = Attempt(pair.asm.FindType,pair.ns + "." + name);
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
                string asm = null;
                if (indexOfAssemblyPrefix != -1)
                {
                    asm = ns.Substring(indexOfAssemblyPrefix + assemblyNamePrefix.Length).Trim();
                    ns = ns.Substring(0, indexOfAssemblyPrefix);
                }

                found = Attempt(x =>
                    context.Configuration.TypeSystem.FindType(x, asm), $"{ns}.{name}");
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