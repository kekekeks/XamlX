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
        IXamlConstructor? TransformArgumentsAndGetConstructor(
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
                {
                    return null;
                }
            }

            for (var c = 0; c < n.Arguments.Count; c++)
            {
                if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, n.Arguments[c], ctor.GetParameterInfo(c), out var arg))
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

                var matchingCtorIsRequired = context.ParentNodes().Any();
                var ctor = TransformArgumentsAndGetConstructor(context, ni);
                if (ctor is not null)
                {
                    return new XamlAstConstructableObjectNode(ni,
                        ni.Type.GetClrTypeReference(), ctor, ni.Arguments, ni.Children);
                }
                else if (!matchingCtorIsRequired)
                {
                    // If matching ctor isn't required and it wasn't found, pass the first possible ctor.
                    // But don't pass any arguments, as compiler doesn't know what to pass at this point.
                    var firstCtor = ni.Type.GetClrType().Constructors[0];
                    return new XamlAstConstructableObjectNode(ni,
                        ni.Type.GetClrTypeReference(), firstCtor, new List<IXamlAstValueNode>(), ni.Children);
                }
                else
                {
                    throw new XamlLoadException(
                        $"Unable to find public constructor for type {ni.Type.GetClrType().GetFqn()}({string.Join(", ", ni.Arguments.Select(at => at.Type.GetClrType().GetFqn()))})",
                        ni);
                }
            }

            return node;
        }
    }
}
