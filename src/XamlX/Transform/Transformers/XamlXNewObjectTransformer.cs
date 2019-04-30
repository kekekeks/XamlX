using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform.Emitters;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlXNewObjectTransformer : IXamlXAstTransformer
    {
        void TransformContent(XamlXAstTransformationContext context, XamlXAstObjectNode ni)
        {
            var valueIndexes = new List<int>();
            for (var c = 0; c < ni.Children.Count; c++)
                if (ni.Children[c] is IXamlXAstValueNode)
                    valueIndexes.Add(c);

            if (valueIndexes.Count == 0)
                return;
            var type = ni.Type.GetClrType();
            IXamlXAstValueNode VNode(int ind) => (IXamlXAstValueNode) ni.Children[ind];

            var contentProperty = context.Configuration.FindContentProperty(type);
            if (contentProperty == null)
            {
                foreach (var ind in valueIndexes)
                    if (XamlXTransformHelpers.TryCallAdd(context,null, type, VNode(ind), out var addCall))
                        ni.Children[ind] = addCall;
                    else
                        throw new XamlXLoadException(
                            $"Type `{type.GetFqn()} does not have content property and suitable Add({VNode(ind).Type.GetClrType().GetFqn()}) not found",
                            VNode(ind));
            }
            else
                XamlXTransformHelpers.GeneratePropertyAssignments(context,
                    new XamlXAstClrProperty(ni, contentProperty), valueIndexes.Count,
                    num => VNode(valueIndexes[num]),
                    (i, v) => ni.Children[valueIndexes[i]] = v);
        }

        IXamlXConstructor TransformArgumentsAndGetConstructor(XamlXAstTransformationContext context,
            XamlXAstObjectNode n)
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
                    throw new XamlXLoadException(
                        $"Unable to find public constructor for type {type.GetFqn()}({string.Join(", ", argTypes.Select(at => at.GetFqn()))})",
                        n);
            }

            for (var c = 0; c < n.Arguments.Count; c++)
            {
                if (!XamlXTransformHelpers.TryGetCorrectlyTypedValue(context, n.Arguments[c], ctor.Parameters[c], out var arg))
                    throw new XamlXLoadException(
                        $"Unable to convert {n.Arguments[c].Type.GetClrType().GetFqn()} to {ctor.Parameters[c].GetFqn()} for constructor of {n.Type.GetClrType().GetFqn()}",
                        n.Arguments[c]);
                n.Arguments[c] = arg;
            }

            return ctor;
        }
        
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstObjectNode ni)
            {
                var t = ni.Type.GetClrType();
                if (t.IsValueType)
                    throw new XamlXLoadException(
                        "Value types can only be loaded via converters. We don't want to mess with ldloca.s, ldflda and other weird stuff",
                        node);

                TransformContent(context, ni);
                var ctor = TransformArgumentsAndGetConstructor(context, ni);
                
                return new XamlXValueWithManipulationNode(ni,
                    new XamlXAstNewClrObjectNode(ni, ni.Type.GetClrTypeReference(), ctor, ni.Arguments),
                    new XamlXObjectInitializationNode(ni,
                        new XamlXManipulationGroupNode(ni)
                        {
                            Children = ni.Children.Cast<IXamlXAstManipulationNode>().ToList()
                        }, ni.Type.GetClrType()));
            }

            return node;
        }
    }
}