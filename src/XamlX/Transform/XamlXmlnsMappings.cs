using System.Collections.Generic;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlXmlnsMappings
    {
        public Dictionary<string, List<(IXamlAssembly asm, string ns)>> Namespaces { get; set; } =
            new Dictionary<string, List<(IXamlAssembly asm, string ns)>>();

        public XamlXmlnsMappings()
        {
            
        }

        public static XamlXmlnsMappings Resolve(IXamlTypeSystem typeSystem, XamlLanguageTypeMappings typeMappings)
        {
            var rv = new XamlXmlnsMappings();
            foreach (var asm in typeSystem.Assemblies)
            foreach (var attr in asm.CustomAttributes)
            foreach (var xmlnsType in typeMappings.XmlnsAttributes)
            {
                if (attr.Type.Equals(xmlnsType))
                {
                    if (attr.Parameters.Count == 2 && attr.Parameters[0] is string xmlns &&
                        attr.Parameters[1] is string clrns)
                    {
                        if (!rv.Namespaces.TryGetValue(xmlns, out var lst))
                            rv.Namespaces[xmlns] = lst = new List<(IXamlAssembly asm, string ns)>();
                        lst.Add((asm, clrns));
                    }
                    else
                        throw new XamlParseException(
                            $"Unexpected parameters for {xmlnsType.GetFqn()} declared on assembly {asm.Name}", 0, 0);
                        
                    break;
                }
            }

            return rv;
        }
    }
}
