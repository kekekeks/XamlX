using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class ConstructableObjectTransformer : IXamlAstTransformer
    {
        IXamlConstructor TransformArgumentsAndGetConstructor(
            AstTransformationContext context,
            XamlAstObjectNode n)
        {
            var type = n.Type.GetClrType();

            var argTypes = n.Arguments.Select(a => a.Type.GetClrType()).ToList();
            var ctor = type.FindConstructor(argTypes);
            if (ctor == null)
            {
                if (argTypes.Count != 0)
                {
                    ctor = type.Constructors.FirstOrDefault(x =>
                        !x.IsStatic && x.IsPublic && x.Parameters.Count == argTypes.Count);

                }

                if (ctor == null)
                    throw new XamlLoadException(
                        $"Unable to find public constructor for type {type.GetFqn()}({string.Join(", ", argTypes.Select(at => at.GetFqn()))})",
                        n);
            }

            for (var c = 0; c < n.Arguments.Count; c++)
            {
                if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, n.Arguments[c], ctor.Parameters[c], out var arg))
                    throw new XamlLoadException(
                        $"Unable to convert {n.Arguments[c].Type.GetClrType().GetFqn()} to {ctor.Parameters[c].GetFqn()} for constructor of {n.Type.GetClrType().GetFqn()}",
                        n.Arguments[c]);
                n.Arguments[c] = arg;
            }

            return ctor;
        }

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni)
            {
                var t = ni.Type.GetClrType();
                if (t.IsValueType)
                    throw new XamlLoadException(
                        "Value types can only be loaded via converters. We don't want to mess with indirect loads and other weird stuff",
                        node);

                var ctor = TransformArgumentsAndGetConstructor(context, ni);
                return new XamlAstConstructableObjectNode(ni,
                    ni.Type.GetClrTypeReference(), ctor, ni.Arguments, ni.Children);
            }

            return node;
        }
    }
}
