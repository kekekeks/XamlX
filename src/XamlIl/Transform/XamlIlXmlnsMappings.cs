using System.Collections.Generic;
using XamlIl.TypeSystem;

namespace XamlIl.Transform
{
    public class XamlIlXmlnsMappings
    {
        public Dictionary<string, List<(IXamlIlAssembly asm, string ns)>> Namespaces { get; set; } =
            new Dictionary<string, List<(IXamlIlAssembly asm, string ns)>>();

        public XamlIlXmlnsMappings()
        {
            
        }

        public static XamlIlXmlnsMappings Resolve(IXamlIlTypeSystem typeSystem, XamlIlLanguageTypeMappings typeMappings)
        {
            var rv = new XamlIlXmlnsMappings();
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
                            rv.Namespaces[xmlns] = lst = new List<(IXamlIlAssembly asm, string ns)>();
                        lst.Add((asm, clrns));
                    }
                    else
                        throw new XamlIlParseException(
                            $"Unexpected parameters for {xmlnsType.GetFqn()} declared on assembly {asm.Name}", 0, 0);
                        
                    break;
                }
            }

            return rv;
        }
    }
}