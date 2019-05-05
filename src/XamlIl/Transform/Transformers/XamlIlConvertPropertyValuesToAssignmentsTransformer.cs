using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlConvertPropertyValuesToAssignmentsTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstXamlPropertyValueNode valueNode)
            {
                var property = valueNode.Property.GetClrProperty();
                var assignments = new List<XamlIlPropertyAssignmentNode>();
                foreach (var v in valueNode.Values)
                {
                    var keyNode = FindAndRemoveKey(v);
                    var arguments = new List<IXamlIlAstValueNode>();

                    if (keyNode != null)
                        arguments.Add(keyNode);
                    arguments.Add(v);



                    XamlIlPropertyAssignmentNode CreateAssignment()
                    {
                        foreach (var setter in property.Setters)
                        {
                            IXamlIlAstValueNode TryConvertParameter(IXamlIlAstValueNode value, IXamlIlType type)
                            {
                                // Don't allow x:Null
                                if (!setter.BinderParameters.AllowNull
                                    && XamlIlPseudoType.Null.Equals(value.Type.GetClrType()))
                                    return null;

                                // Direct cast
                                if (type.IsAssignableFrom(value.Type.GetClrType()))
                                    return value;

                                // Converted
                                if (XamlIlTransformHelpers.TryGetCorrectlyTypedValue(context, value, type,
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
                                    return new XamlIlPropertyAssignmentNode(v, property, new[] {setter}, values);
                                }
                            }


                        }

                        throw new XamlIlLoadException(
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
                            throw new XamlIlLoadException(
                                $"Unable to find a setter that allows multiple assignments to the property {ass.Property.Name} of type {ass.Property.DeclaringType.GetFqn()}",
                                node);
                    }
                }
                
                return new XamlIlManipulationGroupNode(valueNode, assignments);

            }

            return node;
        }

        static IXamlIlAstValueNode FindAndRemoveKey(IXamlIlAstValueNode value)
        {
            IXamlIlAstValueNode keyNode = null;
            
            bool IsKeyDirective(object node) => node is XamlIlAstXmlDirective d
                                                && d.Namespace == XamlNamespaces.Xaml2006 &&
                                                d.Name == "Key";
            void ProcessDirective(object d)
            {
                var directive = (XamlIlAstXmlDirective) d;
                if (directive.Values.Count != 1)
                    throw new XamlIlParseException("Invalid number of arguments for x:Key directive",
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
                
            IXamlIlAstManipulationNode VisitManipulationNode(IXamlIlAstManipulationNode man)
            {
                if (IsKeyDirective(man))
                {
                    ProcessDirective(man);
                    return new XamlIlManipulationGroupNode(man);
                }
                if(man is XamlIlManipulationGroupNode grp)
                    ProcessDirectiveCandidateList(grp.Children);
                if (man is XamlIlObjectInitializationNode init)
                    init.Manipulation = VisitManipulationNode(init.Manipulation);
                return man;
            }
                
            if (value is XamlIlAstObjectNode astObject)
                ProcessDirectiveCandidateList(astObject.Children);
            else if (value is XamlIlValueWithManipulationNode vman)
            {
                vman.Manipulation = VisitManipulationNode(vman.Manipulation);
            }

            return keyNode;

        }
    }
}
