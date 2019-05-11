using System.Collections.Generic;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlXXmlnsMappings
    {
        public Dictionary<string, List<(IXamlXAssembly asm, string ns)>> Namespaces { get; set; } =
            new Dictionary<string, List<(IXamlXAssembly asm, string ns)>>();

        public XamlXXmlnsMappings()
        {
            
        }

        public static XamlXXmlnsMappings Resolve(IXamlXTypeSystem typeSystem, XamlXLanguageTypeMappings typeMappings)
        {
            var rv = new XamlXXmlnsMappings();
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
                            rv.Namespaces[xmlns] = lst = new List<(IXamlXAssembly asm, string ns)>();
                        lst.Add((asm, clrns));
                    }
                    else
                        throw new XamlXParseException(
                            $"Unexpected parameters for {xmlnsType.GetFqn()} declared on assembly {asm.Name}", 0, 0);
                        
                    break;
                }
            }

            return rv;
        }
    }
}
