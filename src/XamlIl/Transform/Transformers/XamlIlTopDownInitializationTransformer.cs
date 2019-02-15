using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlTopDownInitializationTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            var usableAttrs = context.Configuration.TypeMappings.UsableDuringInitializationAttributes;
            if (!(usableAttrs?.Count > 0))
                return node;
            bool UsableDuringInitialization(IXamlIlType type)
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
                IXamlIlAstValueNode checkedNode, out IXamlIlAstValueNode value, out IXamlIlAstManipulationNode deferred)
            {
                value = null;
                deferred = null;
                if (!(checkedNode is XamlIlValueWithManipulationNode manipulation
                      && manipulation.Manipulation is XamlIlObjectInitializationNode initializer
                      && UsableDuringInitialization(manipulation.Value.Type.GetClrType())))
                    return false;
                var local = new XamlIlAstCompilerLocalNode(manipulation.Value, manipulation.Value.Type.GetClrType());
                value = new XamlIlAstLocalInitializationNodeEmitter(local, manipulation.Value, local);
                deferred = new XamlIlAstManipulationImperativeNode(initializer,
                    new XamlIlAstImperativeValueManipulation(initializer, local, initializer));
                return true;
            }
            
            if (node is XamlIlPropertyAssignmentNode assignment)
            {
                if (!TryConvert(assignment.Value, out var nvalue, out var deferred))
                    return node;
                return new XamlIlManipulationGroupNode(assignment)
                {
                    Children =
                    {
                        new XamlIlPropertyAssignmentNode(assignment, assignment.Property, nvalue),
                        deferred
                    }
                };
            }
            else if (node is XamlIlNoReturnMethodCallNode call)
            {
                var deferredNodes = new List<IXamlIlAstManipulationNode>();
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
                    var grp = new XamlIlManipulationGroupNode(call);
                    grp.Children.Add(call);
                    grp.Children.AddRange(deferredNodes);
                    return grp;
                }
            }

            return node;
        }
    }
}