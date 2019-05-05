using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
    public class XamlXConvertPropertyValuesToAssignmentsTransformer : IXamlXAstTransformer
    {
        public IXamlXAstNode Transform(XamlXAstTransformationContext context, IXamlXAstNode node)
        {
            if (node is XamlXAstXamlPropertyValueNode valueNode)
            {
                var property = valueNode.Property.GetClrProperty();
                var assignments = new List<XamlXPropertyAssignmentNode>();
                foreach (var v in valueNode.Values)
                {
                    var keyNode = FindAndRemoveKey(v);
                    var arguments = new List<IXamlXAstValueNode>();

                    if (keyNode != null)
                        arguments.Add(keyNode);
                    arguments.Add(v);



                    XamlXPropertyAssignmentNode CreateAssignment()
                    {
                        foreach (var setter in property.Setters)
                        {
                            IXamlXAstValueNode TryConvertParameter(IXamlXAstValueNode value, IXamlXType type)
                            {
                                // Don't allow x:Null
                                if (!setter.BinderParameters.AllowNull
                                    && XamlXPseudoType.Null.Equals(value.Type.GetClrType()))
                                    return null;

                                // Direct cast
                                if (type.IsAssignableFrom(value.Type.GetClrType()))
                                    return value;

                                // Converted
                                if (XamlXTransformHelpers.TryGetCorrectlyTypedValue(context, value, type,
                                    out var converted))
                                    return converted;

                                // Upcast from System.Object
                                if (value.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.Object))
                                    return value;

                                return null;
                            }

                            if (arguments.Count == setter.Parameters.Count)
                            {
                                var matches = true;
                                var values = arguments.ToList();
                                for (var c = 0; c < arguments.Count; c++)
                                {
                                    if ((values[c] = TryConvertParameter(values[c], setter.Parameters[c])) == null)
                                    {
                                        matches = false;
                                        break;
                                    }
                                }

                                if (matches)
                                {
                                    return new XamlXPropertyAssignmentNode(v, property, new[] {setter}, values);
                                }
                            }


                        }

                        throw new XamlXLoadException(
                            $"Unable to find suitable setter or adder for property {property.Name} of type {property.DeclaringType.GetFqn()} for argument {v.Type.GetClrType().GetFqn()}"
                            + (keyNode != null ? $" and x:Key of type {keyNode.Type.GetClrType()}" : null), v);
                    }

                    assignments.Add(CreateAssignment());
                }

                if (assignments.Count == 1)
                    return assignments[0];

                if (assignments.Count > 1)
                {
                    // Skip the first one, since we only care about further setters, e. g. the following is perfectly valid:
                    // <Foo.Bar>
                    //   <SomeList/>
                    //   <ListItem/>
                    //   <ListItem/>
                    // </Foo.Bar>
                    // <SomeList/> would be foo.Bar = new SomeList() and <ListItem/> would be foo.Bar.Add(new ListItem());
                    foreach(var ass in assignments.Skip(1))
                    {
                        ass.PossibleSetters = ass.PossibleSetters.Where(s => s.BinderParameters.AllowMultiple).ToList();
                        if (ass.PossibleSetters.Count == 0)
                            throw new XamlXLoadException(
                                $"Unable to find a setter that allows multiple assignments to the property {ass.Property.Name} of type {ass.Property.DeclaringType.GetFqn()}",
                                node);
                    }
                }
                
                return new XamlXManipulationGroupNode(valueNode, assignments);

            }

            return node;
        }

        static IXamlXAstValueNode FindAndRemoveKey(IXamlXAstValueNode value)
        {
            IXamlXAstValueNode keyNode = null;
            
            bool IsKeyDirective(object node) => node is XamlXAstXmlDirective d
                                                && d.Namespace == XamlNamespaces.Xaml2006 &&
                                                d.Name == "Key";
            void ProcessDirective(object d)
            {
                var directive = (XamlXAstXmlDirective) d;
                if (directive.Values.Count != 1)
                    throw new XamlXParseException("Invalid number of arguments for x:Key directive",
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
                
            IXamlXAstManipulationNode VisitManipulationNode(IXamlXAstManipulationNode man)
            {
                if (IsKeyDirective(man))
                {
                    ProcessDirective(man);
                    return new XamlXManipulationGroupNode(man);
                }
                if(man is XamlXManipulationGroupNode grp)
                    ProcessDirectiveCandidateList(grp.Children);
                if (man is XamlXObjectInitializationNode init)
                    init.Manipulation = VisitManipulationNode(init.Manipulation);
                return man;
            }
                
            if (value is XamlXAstObjectNode astObject)
                ProcessDirectiveCandidateList(astObject.Children);
            else if (value is XamlXValueWithManipulationNode vman)
            {
                vman.Manipulation = VisitManipulationNode(vman.Manipulation);
            }

            return keyNode;

        }
    }
}
