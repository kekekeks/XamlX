using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlXTopDownInitializationTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            var usableAttrs = context.Configuration.TypeMappings.UsableDuringInitializationAttributes;
            if (!(usableAttrs?.Count > 0))
                return node;
            bool UsableDuringInitialization(IXamlXType type)
            {
                foreach (var attr in type.CustomAttributes)
                {
                    foreach (var attrType in usableAttrs)
                    {
                        if (attr.Type.Equals(attrType))
                            return attr.Parameters.Count == 0 || attr.Parameters[0] as bool? == true;
                    }
                }

                if (type.BaseType != null)
                    return UsableDuringInitialization(type.BaseType);
                return false;
            }

            bool TryConvert(
                IXamlXAstValueNode checkedNode, out IXamlXAstValueNode value, out IXamlXAstManipulationNode deferred)
            {
                value = null;
                deferred = null;
                if (!(checkedNode is XamlXValueWithManipulationNode manipulation
                      && manipulation.Manipulation is XamlXObjectInitializationNode initializer
                      && UsableDuringInitialization(manipulation.Value.Type.GetClrType())))
                    return false;
                initializer.SkipBeginInit = true;
                var local = new XamlXAstCompilerLocalNode(manipulation.Value, manipulation.Value.Type.GetClrTypeReference());
                value = new XamlXValueNodeWithBeginInit(new XamlXAstLocalInitializationNodeEmitter(local, manipulation.Value, local));
                deferred = new XamlXAstManipulationImperativeNode(initializer,
                    new XamlXAstImperativeValueManipulation(initializer, local, initializer));
                return true;
            }
            
            if (node is XamlXPropertyAssignmentNode assignment)
            {
                if (!TryConvert(assignment.Values.Last(), out var nvalue, out var deferred))
                    return node;
                
                assignment.Values[assignment.Values.Count - 1] = nvalue;
                return new XamlXManipulationGroupNode(assignment)
                {
                    Children =
                    {
                        assignment,
                        deferred
                    }
                };
            }
            else if (node is XamlXNoReturnMethodCallNode call)
            {
                var deferredNodes = new List<IXamlXAstManipulationNode>();
                for (var c = 0; c < call.Arguments.Count; c++)
                {
                    var arg = call.Arguments[c];
                    if (TryConvert(arg, out var narg, out var deferred))
                    {
                        call.Arguments[c] = narg;
                        deferredNodes.Add(deferred);
                    }
                }

                if (deferredNodes.Count != 0)
                {
                    var grp = new XamlXManipulationGroupNode(call);
                    grp.Children.Add(call);
                    grp.Children.AddRange(deferredNodes);
                    return grp;
                }
            }

            return node;
        }
    }
}
