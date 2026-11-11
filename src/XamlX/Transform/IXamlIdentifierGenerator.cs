using XamlX.Emit;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlIdentifierGenerator
    {
        GeneratedIdentifier CreateIdentifierFromPrefix(string prefix);
        GeneratedIdentifier CreateIdentifierFromPreferredName(string name, int nameUseCount);
    }

#if !XAMLX_INTERNAL
    public
#endif
    abstract class GeneratedIdentifier
    {
        public abstract string Name { get; }
        public abstract string AppendSuffix(string suffix);

        public static implicit operator string(GeneratedIdentifier identifier) => identifier.Name;
    }

    internal sealed class ReadableGeneratedIdentifier : GeneratedIdentifier
    {
        private readonly string _name;

        public ReadableGeneratedIdentifier(string name) => _name = name;
        public override string Name => _name;
        public override string AppendSuffix(string suffix) => _name + suffix;
    }

}
