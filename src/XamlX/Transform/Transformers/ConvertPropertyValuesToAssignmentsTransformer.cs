using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class ConvertPropertyValuesToAssignmentsTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXamlPropertyValueNode valueNode)
            {
                var property = valueNode.Property.GetClrProperty();
                var assignments = new List<XamlPropertyAssignmentNode>();
                foreach (var v in valueNode.Values)
                {
                    var keyNode = FindAndRemoveKey(v);
                    var arguments = new List<IXamlAstValueNode>();

                    if (keyNode != null)
                        arguments.Add(keyNode);

                    arguments.Add(v);
                    assignments.Add(new XamlPropertyAssignmentNode(v, property, arguments));
                }

                if (assignments.Count == 1)
                    return assignments[0];
                
                return new XamlManipulationGroupNode(valueNode, assignments);

            }

            return node;
        }

        static IXamlAstValueNode FindAndRemoveKey(IXamlAstValueNode value)
        {
            IXamlAstValueNode keyNode = null;
            
            bool IsKeyDirective(object node) => node is XamlAstXmlDirective d
                                                && d.Namespace == XamlNamespaces.Xaml2006 &&
                                                d.Name == "Key";
            void ProcessDirective(object d)
            {
                var directive = (XamlAstXmlDirective) d;
                if (directive.Values.Count != 1)
                    throw new XamlParseException("Invalid number of arguments for x:Key directive",
                        directive);
                keyNode = directive.Values[0];
            }

               
            void ProcessDirectiveCandidateList(IList nodes)
            {
                var d = nodes.OfType<object>().FirstOrDefault(IsKeyDirective);
                if (d != null)
                {
                    ProcessDirective(d);
                    nodes.Remove(d);
                }
            }
                
            IXamlAstManipulationNode VisitManipulationNode(IXamlAstManipulationNode man)
            {
                if (IsKeyDirective(man))
                {
                    ProcessDirective(man);
                    return new XamlManipulationGroupNode(man);
                }
                if(man is XamlManipulationGroupNode grp)
                    ProcessDirectiveCandidateList(grp.Children);
                if (man is XamlObjectInitializationNode init)
                    init.Manipulation = VisitManipulationNode(init.Manipulation);
                return man;
            }

            var probe = (value is XamlValueWithSideEffectNodeBase side) ? side.Value : value;
                
            if (probe is XamlAstObjectNode astObject)
                ProcessDirectiveCandidateList(astObject.Children);
            else if (value is XamlValueWithManipulationNode vman)
            {
                vman.Manipulation = VisitManipulationNode(vman.Manipulation);
            }
            else if (value is XamlMarkupExtensionNode mext)
            {
                return FindAndRemoveKey(mext.Value);
            }

            return keyNode;

        }
    }
}
