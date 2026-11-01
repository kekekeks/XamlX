using System;
using XamlX.Emit;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlIdentifierGenerator
    {
        string GenerateIdentifierPart();
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IGeneratedIdentifierFactory : IXamlIdentifierGenerator
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

#if !XAMLX_INTERNAL
    public
#endif
    static class GeneratedIdentifierExtensions
    {
        public static GeneratedIdentifier CreateIdentifierFromPrefix(this IXamlIdentifierGenerator generator, string prefix)
            => generator is IGeneratedIdentifierFactory factory
                ? factory.CreateIdentifierFromPrefix(prefix)
                : new ReadableGeneratedIdentifier(prefix + generator.GenerateIdentifierPart());

        public static GeneratedIdentifier CreateIdentifierFromPreferredName(
            this IXamlIdentifierGenerator generator, string name, int nameUseCount)
            => generator is IGeneratedIdentifierFactory factory
                ? factory.CreateIdentifierFromPreferredName(name, nameUseCount)
                : new ReadableGeneratedIdentifier(nameUseCount == 0
                    ? name
                    : name + "_" + generator.GenerateIdentifierPart());
    }

#if !XAMLX_INTERNAL
    public
#endif
    class GuidIdentifierGenerator : IGeneratedIdentifierFactory
    {
        public string GenerateIdentifierPart() => Guid.NewGuid().ToString().Replace("-","");

        public GeneratedIdentifier CreateIdentifierFromPrefix(string prefix)
            => new ReadableGeneratedIdentifier(prefix + GenerateIdentifierPart());

        public GeneratedIdentifier CreateIdentifierFromPreferredName(string name, int nameUseCount)
            => new ReadableGeneratedIdentifier(nameUseCount == 0
                ? name
                : name + "_" + GenerateIdentifierPart());
    }
}
