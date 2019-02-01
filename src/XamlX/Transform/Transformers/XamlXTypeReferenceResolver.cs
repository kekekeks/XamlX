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
            if (typeArguments.Count != 0)
                name += "`" + typeArguments.Count;
                
            const string clrNamespace = "clr-namespace:";
            const string assemblyNamePrefix = "assembly=";
            
            
            
            IXamlXType found = null;
            
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
                var asm = context.Configuration.DefaultAssembly;
                var ns = xmlns.Substring(clrNamespace.Length);

                // Parse `?assembly=` part if present
                var indexOfQuestionMark = ns.IndexOf('?');
                if (indexOfQuestionMark != -1)
                {
                    var qs = ns.Substring(indexOfQuestionMark + 1);
                    ns = ns.Substring(0, indexOfQuestionMark);
                    foreach (var pair in qs.Split('&'))
                        if (pair.StartsWith(assemblyNamePrefix))
                        {
                            var assemblyName = pair.Substring(assemblyNamePrefix.Length);
                            var foundAsm = context.Configuration.TypeSystem.FindAssembly(assemblyName);
                            if (foundAsm == null)
                            {
                                if (context.StrictMode)
                                    throw new XamlXParseException(
                                        $"Unable to find assembly {assemblyName} referenced by {xmlns}",
                                        lineInfo);
                                else
                                    return null;
                            }

                            break;
                        }
                }
                found = asm.FindType($"{ns}.{name}");
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