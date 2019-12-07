using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Transformers
{
#if !XAMLIL_INTERNAL
    public
#endif
    class XamlIlConvertPropertyValuesToAssignmentsTransformer : IXamlIlAstTransformer
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


                    // Pre-filter setters by non-last argument
                    var filteredSetters = property.Setters.Where(s => s.Parameters.Count == arguments.Count)
                        .ToList();
                    
                    if (arguments.Count > 1)
                    {
                        for (var c = 0; c < arguments.Count - 1; c++)
                        {
                            IXamlIlType convertedTo = null;
                            for (var s = 0; s < filteredSetters.Count;)
                            {
                                var setter = filteredSetters[s];
                                if (convertedTo == null)
                                {
                                    if (!XamlIlTransformHelpers.TryGetCorrectlyTypedValue(context, arguments[c],
                                        setter.Parameters[c], out var converted))
                                    {
                                        filteredSetters.RemoveAt(c);
                                        continue;
                                    }
                                    else
                                    {
                                        convertedTo = converted.Type.GetClrType();
                                        arguments[c] = converted;
                                    }
                                }
                                else
                                {
                                    if (!setter.Parameters[c].IsAssignableFrom(convertedTo))
                                        throw new XamlIlLoadException(
                                            $"Runtime setter selection is not supported for non-last setter arguments (e. g. x:Key) and can not downcast argument {c} of the setter from {convertedTo} to {setter.Parameters[c]}",
                                            arguments[c]);
                                }
                                s++;
                            }
                        }
                    }

                    XamlIlPropertyAssignmentNode CreateAssignment()
                    {
                        var matchedSetters = new List<IXamlIlPropertySetter>();
                        foreach (var setter in filteredSetters)
                        {
                            bool CanAssign(IXamlIlAstValueNode value, IXamlIlType type)
                            {
                                // Don't allow x:Null
                                if (!setter.BinderParameters.AllowXNull
                                    && XamlIlPseudoType.Null.Equals(value.Type.GetClrType()))
                                    return false;

                                // Direct cast
                                if (type.IsAssignableFrom(value.Type.GetClrType()))
                                    return true;

                                // Upcast from System.Object
                                if (value.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.Object))
                                    return true;

                                return false;
                            }

                            var valueArgIndex = arguments.Count - 1;
                            var valueArg = arguments[valueArgIndex];
                            var setterType = setter.Parameters[valueArgIndex];
                            
                            if(CanAssign(valueArg, setterType))
                                matchedSetters.Add(setter);
                            // Converted value have more priority than custom setters, so we just create a setter without an alternative
                            else if (XamlIlTransformHelpers.TryConvertValue(context, valueArg, setterType, property,
                                out var converted))
                            {
                                
                                arguments[valueArgIndex] = converted;
                                return new XamlIlPropertyAssignmentNode(valueNode,
                                    property, new[] {setter}, arguments);
                            }
                        }

                        if (matchedSetters.Count > 0)
                            return new XamlIlPropertyAssignmentNode(v, property, matchedSetters, arguments);

                        throw new XamlIlLoadException(
                            $"Unable to find suitable setter or adder for property {property.Name} of type {property.DeclaringType.GetFqn()} for argument {v.Type.GetClrType().GetFqn()}"
                            + (keyNode != null ? $" and x:Key of type {keyNode.Type.GetClrType()}" : null)
                            + ", available setter parameter lists are:\n" + string.Join("\n",
                                filteredSetters.Select(setter =>
                                    string.Join(", ", setter.Parameters.Select(p => p.FullName))))
                            , v);
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

            var probe = (value is XamlIlValueWithSideEffectNodeBase side) ? side.Value : value;

            if (probe is XamlIlAstObjectNode astObject)
                ProcessDirectiveCandidateList(astObject.Children);
            else if (value is XamlIlValueWithManipulationNode vman)
            {
                vman.Manipulation = VisitManipulationNode(vman.Manipulation);
            }

            return keyNode;

        }
    }
}
