using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    static class NamespaceInfoHelper
    {
        public class NamespaceResolveResult
        {
            public string ClrNamespace { get; set; }
            public IXamlAssembly Assembly { get; set; }
            public string AssemblyName { get; set; }
        }
        
        public static List<NamespaceResolveResult> TryResolve(TransformerConfiguration config, string xmlns)
        {
            if (config.XmlnsMappings.Namespaces.TryGetValue(xmlns, out var lst))
            {
                return lst.Select(p => new NamespaceResolveResult
                {
                    ClrNamespace = p.ns,
                    Assembly = p.asm
                }).ToList();
            }
            
            const string clrNamespace = "clr-namespace:";
            const string assemblyNamePrefix = ";assembly=";
            if (xmlns.StartsWith(clrNamespace))
            {
                var ns = xmlns.Substring(clrNamespace.Length);
                
                var indexOfAssemblyPrefix = ns.IndexOf(assemblyNamePrefix, StringComparison.Ordinal);
                string asm = config.DefaultAssembly?.Name;
                if (indexOfAssemblyPrefix != -1)
                {
                    asm = ns.Substring(indexOfAssemblyPrefix + assemblyNamePrefix.Length).Trim();
                    ns = ns.Substring(0, indexOfAssemblyPrefix);
                }
                return new List<NamespaceResolveResult>
                {
                    new NamespaceResolveResult
                    {
                        ClrNamespace = ns,
                        AssemblyName = asm
                    }
                };
            }

            return null;
        }
    }
}
