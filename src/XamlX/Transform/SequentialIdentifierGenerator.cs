using System.Globalization;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    class SequentialIdentifierGenerator : IXamlIdentifierGenerator
    {
        private long _nextId = 1;

        private string NextId() => (_nextId++).ToString(CultureInfo.InvariantCulture);

        public GeneratedIdentifier CreateIdentifierFromPrefix(string prefix)
            => new ReadableGeneratedIdentifier(prefix + NextId());

        public GeneratedIdentifier CreateIdentifierFromPreferredName(string name, int nameUseCount)
            => new ReadableGeneratedIdentifier(nameUseCount == 0 ? name : name + "_" + NextId());
    }
}
