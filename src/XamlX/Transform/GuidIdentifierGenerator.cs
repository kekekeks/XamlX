using System;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class GuidIdentifierGenerator : IXamlIdentifierGenerator
    {
        private static string GenerateIdentifierPart() => Guid.NewGuid().ToString().Replace("-", "");

        public GeneratedIdentifier CreateIdentifierFromPrefix(string prefix)
            => new ReadableGeneratedIdentifier(prefix + GenerateIdentifierPart());

        public GeneratedIdentifier CreateIdentifierFromPreferredName(string name, int nameUseCount)
            => new ReadableGeneratedIdentifier(nameUseCount == 0
                ? name
                : name + "_" + GenerateIdentifierPart());
    }
}
